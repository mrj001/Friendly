﻿#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using Friendly.Library.Pollard;
using Friendly.Library.Utility;

// NOTE: For references, see the file QuadraticSieve.cs

namespace Friendly.Library.QuadraticSieve
{
   public class Relations3P : IRelations
   {
      /// <summary>
      /// The set of fully factored Relations found during sieving and combining.
      /// </summary>
      private readonly List<Relation> _relations;
      private readonly object _lockRelations = new object();

      private readonly int _factorBaseSize;
      private readonly int _maxFactor;

      /// <summary>
      /// The maximum value that any large prime will be allowed to take on.
      /// This will be smaller than _maxSinglePrime as when the Single Primes
      /// get larger the probability of them being useful drops dramatically.
      /// </summary>
      private readonly long _maxLargePrime;

      /// <summary>
      /// The square of _maxFactor.  This is the upper limit of a
      /// residual that could be conceivably used for a Single Large Prime
      /// Partial relation.  It is the lower limit of the range of
      /// residuals that could be used for Double Large Prime Partial Relations.
      /// </summary>
      private readonly long _maxSinglePrime;

      /// <summary>
      /// The maximum value of the residual that will be considered for inclusion
      /// in a Two Large Primes Relation.
      /// </summary>
      private readonly long _maxTwoPrimes;

      /// <summary>
      /// The maximum value of the residual that will be considered for inclusion
      /// in a Three Large Primes Relation.
      /// </summary>
      private readonly BigInteger _maxThreePrimes;

      //====================================================================
      // BEGIN member data for cycle counting
      private int _count;
      private int _nextCountThreshold;
      private readonly int _incrementCountThreshold;
      private int _componentCount;
      private int _edgeCount;
      private readonly object _lockCount = new object();
      private Task? _taskCount = null;
      // END member data for cycle counting
      //====================================================================

      //====================================================================
      // BEGIN member data for tracking partial relations
      private readonly List<TPRelation> _singletons = new();
      private readonly List<TPRelation> _doubles = new();
      private readonly List<TPRelation> _triples = new();
      private readonly object _lockPartials = new object();
      // END member data for tracking partial relations
      //====================================================================

      private const int InitialCapacity = 1 << 18;
      public const string TypeNodeName = Relations2P.TypeNodeName;
      public const string TypeNodeValue = "Relations3P";
      private const string LargePrimeNodeName = "maxLargePrime";
      private const string SingleLargePrimeNodeName = "maxSingleLargePrime";
      private const string TwoLargePrimeNodeName = "maxTwoLargePrimes";
      private const string ThreeLargePrimeNodeName = "maxThreeLargePrimes";
      private const string StatisticsNodeName = "statistics";
      private const string StatisticNodeName = "statistic";
      private const string RelationsNodeName = "relations";
      private const string PartialRelationsNodeName = "partialrelations";

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The value of the largest prime in the Factor Base.</param>
      public Relations3P(int factorBaseSize, int maxFactor)
      {
         _relations = new();
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;

         _maxLargePrime = 100_000_000;
         _maxSinglePrime = (long)_maxFactor * _maxFactor;
         _maxTwoPrimes = _maxSinglePrime * _maxFactor;
         _maxThreePrimes = ((BigInteger)_maxTwoPrimes) * _maxFactor;

         _count = 0;
         _incrementCountThreshold = _factorBaseSize / 10;
         _nextCountThreshold = _incrementCountThreshold;

         StartBackgroundTasks();
      }

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The value of the largest prime in the Factor Base.</param>
      /// <param name="rdr">An XML Reader positioned at the &lt;maxLargePrime&gt; node..</param>
      public Relations3P(int factorBaseSize, int maxFactor, XmlReader rdr)
      {
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;

         rdr.ReadStartElement(LargePrimeNodeName);
         _maxLargePrime = SerializeHelper.ParseLongNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(SingleLargePrimeNodeName);
         _maxSinglePrime = SerializeHelper.ParseLongNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(TwoLargePrimeNodeName);
         _maxTwoPrimes = SerializeHelper.ParseLongNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(ThreeLargePrimeNodeName);
         _maxThreePrimes = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         _count = 0;
         _incrementCountThreshold = _factorBaseSize / 10;
         _nextCountThreshold = _incrementCountThreshold;

         // Restore stats that we need to round trip
         List<Statistic> statistics = new();
         rdr.ReadStartElement(StatisticsNodeName);
         while (rdr.IsStartElement(StatisticNodeName))
            statistics.Add(new Statistic(rdr));
         rdr.ReadEndElement();

         // Read the full Relations
         _relations = new();
         rdr.ReadStartElement(RelationsNodeName);
         while (rdr.IsStartElement("r"))
            _relations.Add(new Relation(rdr));
         rdr.ReadEndElement();

         // Read the Partial relations (1-, 2-, and 3-prime relations)
         // NOTE: _singletons was sorted prior to writing to the save file, so
         //     it is sorted when read back.
         rdr.ReadStartElement(PartialRelationsNodeName);
         while (rdr.IsStartElement("r"))
         {
            TPRelation tpr = new TPRelation(rdr);
            if (tpr.Count == 1)
               _singletons.Add(tpr);
            else if (tpr.Count == 2)
               _doubles.Add(tpr);
            else
               _triples.Add(tpr);
         }
         rdr.ReadEndElement();

         StartBackgroundTasks();

         rdr.ReadEndElement();
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // Shutdown the Factoring task so it is safe to serialize this object.
         StopBackgroundTasks();
      }

      /// <inheritdoc />
      public void Serialize(XmlWriter writer, string name)
      {
         writer.WriteStartElement(name);

         writer.WriteElementString(TypeNodeName, TypeNodeValue);
         writer.WriteElementString(LargePrimeNodeName, _maxLargePrime.ToString());
         writer.WriteElementString(SingleLargePrimeNodeName, _maxSinglePrime.ToString());
         writer.WriteElementString(TwoLargePrimeNodeName, _maxTwoPrimes.ToString());
         writer.WriteElementString(ThreeLargePrimeNodeName, _maxThreePrimes.ToString());

         writer.WriteStartElement(StatisticsNodeName);
         writer.WriteEndElement();

         writer.WriteStartElement(RelationsNodeName);
         foreach(Relation r in _relations)
            r.Serialize(writer, "r");
         writer.WriteEndElement();

         writer.WriteStartElement(PartialRelationsNodeName);
         foreach (TPRelation tpr in _singletons)
            tpr.Serialize(writer, "r");
         foreach (TPRelation tpr in _doubles)
            tpr.Serialize(writer, "r");
         foreach (TPRelation tpr in _triples)
            tpr.Serialize(writer, "r");
         writer.WriteEndElement();

         writer.WriteEndElement();
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         if (reason == SerializationReason.SaveState)
            StartBackgroundTasks();
      }

      #region Control of Background Tasks
      private void StartBackgroundTasks()
      {
         // We do not start the _taskCount here.
      }

      private void StopBackgroundTasks()
      {
         _taskCount?.Wait();
         _taskCount?.Dispose();
         _taskCount = null;
      }
      #endregion

      #region Factoring of incoming residuals
      /// <summary>
      /// Factors residuals and creates TPRelation objects.
      /// </summary>
      /// <param name="QofX"></param>
      /// <param name="x"></param>
      /// <param name="exponentVector"></param>
      /// <param name="residual"></param>
      /// <exception cref="ApplicationException">The failure of an invariant implies a bug.</exception>
      /// <remarks>
      /// <para>
      /// This method runs in a Sieving Task.
      /// </para>
      /// </remarks>
      private void FactorRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector, BigInteger residual)
      {
         if (residual == BigInteger.One)
         {
            // Add a fully factored Relation.
            Relation newRelation = new Relation(QofX, x, exponentVector);
            lock (_lockRelations)
               _relations.Add(newRelation);
            return;
         }
         else if (residual < _maxFactor)
         {
            // We occasionally see residual values that equal the multiplier.
            // Discard these.
            return;
         }
         else if (residual <= _maxSinglePrime)
         {
            // If the Single Large Prime is too large, discard it.
            if (residual > _maxLargePrime)
               return;

            TPRelation relation = new TPRelation(QofX, x, exponentVector,
               new long[] { (long)residual });
            AddSingletonRelation(relation);
            return;
         }
         else if (residual <= _maxTwoPrimes && !Primes.IsPrime(residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger p1, BigInteger p2) = rho.Factor(residual);

            // If either factor is larger than _maxLargePrime, discard.
            if (p1 > _maxLargePrime || p2 > _maxLargePrime)
               return;

            if (p1 != p2)
            {
               TPRelation relation = new TPRelation(QofX, x, exponentVector,
                  new long[] { (long)p1, (long)p2 });
               _doubles.Add(relation);
               return;
            }
            else
            {
               Relation newRelation = new Relation(QofX, x, exponentVector, RelationOrigin.TwoLargePrimes);
               lock(_lockRelations)
                  _relations.Add(newRelation);
               return;
            }
         }
         else if (residual < _maxThreePrimes && !Primes.IsPrime(residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger f1, BigInteger f2) = rho.Factor(residual);
            BigInteger f3;
            bool f1Prime = Primes.IsPrime(f1);
            bool f2Prime = Primes.IsPrime(f2);

            if (!f1Prime && !f2Prime)
               // Reject: too many prime factors.
               // This should not happen.  It implies that there are 4 prime
               // factors of a number > _maxFactor && < _maxThreePrimes.
               throw new ApplicationException();

            if (f1Prime && f2Prime)
               // Reject:  They're both prime, but one of them must be bigger
               // than _maxSinglePrime
               return;

            if (f1Prime)
            {  // f2 is composite
               rho = new PollardRho();
               (f2, f3) = rho.Factor(f2);
            }
            else
            {  // f1 is composite
               rho = new PollardRho();
               (f1, f3) = rho.Factor(f1);
            }

            // If any factor is larger than _maxLargePrime, discard the
            // relation
            if (f1 > _maxLargePrime || f2 > _maxLargePrime || f3 > _maxLargePrime)
               return;

            // Check if a prime occurs twice
            if (f1 == f2)
            {
               TPRelation relation = new TPRelation(QofX, x, exponentVector,
                  new long[] { (long)f3 }, RelationOrigin.ThreeLargePrimes);
               lock(_lockPartials)
                  _singletons.Add(relation);
               return;
            }
            else if (f1 == f3)
            {
               TPRelation relation = new TPRelation(QofX, x, exponentVector,
                  new long[] { (long)f2 }, RelationOrigin.ThreeLargePrimes);
               lock (_lockPartials)
                  _singletons.Add(relation);
               return;
            }
            else if (f2 == f3)
            {
               TPRelation relation = new TPRelation(QofX, x, exponentVector,
                  new long[] { (long)f1 }, RelationOrigin.ThreeLargePrimes);
               lock (_lockPartials)
                  _singletons.Add(relation);
               return;
            }
            else
            {
               TPRelation relation = new TPRelation(QofX, x, exponentVector,
                  new long[] { (long)f1, (long)f2, (long)f3 });
               _triples.Add(relation);
               return;
            }
         }
      }

      /// <summary>
      /// Adds a TPRelation with a single Large Prime (a "singleton").
      /// </summary>
      /// <param name="relation"></param>
      /// <remarks>
      /// <para>
      /// This runs in the _actionSingleton Action Block.  It will also be called
      /// on the main thread during construction when resuming from a saved
      /// state.
      /// </para>
      /// </remarks>
      private void AddSingletonRelation(TPRelation relation)
      {
         int index;

         lock (_lockPartials)
            index = _singletons.BinarySearch(relation, new CompareSingletons());

         if (index >= 0)
         {
            // The new Singleton matches an existing one, so create a full
            // relation.
            TPRelation otherRelation;
            lock (_lockPartials)
               otherRelation = _singletons[index];
            BigInteger qOfX = relation.QOfX * otherRelation.QOfX;
            BigInteger x = relation.X * otherRelation.X;
            BigBitArray exponentVector = new BigBitArray(relation.ExponentVector);
            exponentVector.Xor(otherRelation.ExponentVector);
            Relation newRelation = new Relation(qOfX, x, exponentVector, RelationOrigin.OneLargePrime);
            lock (_lockRelations)
               _relations.Add(newRelation);
         }
         else
         {
            // The new Singleton is not present in the list of Singletons; add it.
            index ^= ~0;
            lock (_lockPartials)
               _singletons.Insert(index, relation);
         }
      }

      private class CompareSingletons : IComparer<TPRelation>
      {
         public int Compare(TPRelation? x, TPRelation? y)
         {
            long rv = x![0] - y![0];
            if (rv > int.MaxValue)
               rv = int.MaxValue;
            else if (rv < int.MinValue)
               rv = int.MinValue;

            return (int)rv;
         }
      }

      private void CountCycles()
      {
         List<TPRelation> tmpList;
         List<TPRelation> singletons;
         List<TPRelation> doubles;
         List<TPRelation> triples;
         int sz;

         lock (_lockPartials)
         {
            singletons = new List<TPRelation>(_singletons.Count);
            singletons.AddRange(_singletons);
            doubles = new List<TPRelation>(_doubles.Count);
            doubles.AddRange(_doubles);
            triples = new List<TPRelation>(_triples.Count);
            triples.AddRange(_triples);
         }

         bool changes;
         int usefulPrimeCount;

         do
         {
            changes = false;

            sz = singletons.Count + doubles.Count + triples.Count;
            Dictionary<long, int> usefulPrimes = new Dictionary<long, int>(sz);

            // Count primes
            // singletons was constructed to contain only unique primes.
            foreach (TPRelation tpr in singletons)
               usefulPrimes.Add(tpr[0], 1);
            foreach (TPRelation tpr in doubles)
            {
               CountPrime(usefulPrimes, tpr[0]);
               CountPrime(usefulPrimes, tpr[1]);
            }
            foreach (TPRelation tpr in triples)
            {
               CountPrime(usefulPrimes, tpr[0]);
               CountPrime(usefulPrimes, tpr[1]);
               CountPrime(usefulPrimes, tpr[2]);
            }

            // Build new Lists containing only "useful" primes
            tmpList = new List<TPRelation>(singletons.Count);
            tmpList.AddRange(singletons.Where(x => usefulPrimes[x[0]] > 1));
            changes |= (tmpList.Count != singletons.Count);
            singletons = tmpList;

            tmpList = new List<TPRelation>(doubles.Count);
            tmpList.AddRange(doubles.Where(x => usefulPrimes[x[0]] > 1 && usefulPrimes[x[1]] > 1));
            changes |= (tmpList.Count != doubles.Count);
            doubles = tmpList;

            tmpList = new List<TPRelation>(triples.Count);
            tmpList.AddRange(triples.Where(x => usefulPrimes[x[0]] > 1 && usefulPrimes[x[1]] > 1 && usefulPrimes[x[2]] > 1));
            changes |= (tmpList.Count != triples.Count);
            triples = tmpList;

            usefulPrimeCount = usefulPrimes.Count;
         } while (changes);

         // Count components
         sz = singletons.Count + doubles.Count + triples.Count;
         Dictionary<long, long> components = new(sz);
         int componentCount = 0;
         int edgeCount = 0;
         components.Add(1, 1);

         foreach(TPRelation tpr in singletons)
            Union(1, tpr[0], components, ref componentCount);
         edgeCount = 3 * singletons.Count;

         foreach(TPRelation tpr in doubles)
         {
            Union(1, tpr[0], components, ref componentCount);
            Union(1, tpr[1], components, ref componentCount);
            Union(tpr[0], tpr[1], components, ref componentCount);
            edgeCount += 3;
         }

         foreach (TPRelation tpr in triples)
         {
            Union(tpr[0], tpr[1], components, ref componentCount);
            Union(tpr[0], tpr[2], components, ref componentCount);
            Union(tpr[1], tpr[2], components, ref componentCount);
            edgeCount += 3;
         }

         int count = componentCount + edgeCount - usefulPrimeCount
            - 2 * (singletons.Count + doubles.Count + triples.Count);
         if (count < 0)
            throw new ApplicationException();

         lock(_lockCount)
         {
            _count = count;
            _nextCountThreshold += _incrementCountThreshold;
            _componentCount = componentCount;
            _edgeCount = edgeCount;
         }
      }

      private static void CountPrime(Dictionary<long, int> usefulPrimes, long prime)
      {
         if (usefulPrimes.ContainsKey(prime))
            usefulPrimes[prime]++;
         else
            usefulPrimes.Add(prime, 1);
      }

      /// <summary>
      /// Finds the "root" of the current set (component) of vertices.
      /// </summary>
      /// <param name="prime">The prime to find the root of.</param>
      /// <param name="components"></param>
      /// <param name="componentCount"></param>
      /// <returns>The prime which is serving as the root of the given prime's component.</returns>
      private static long Find(long prime, Dictionary<long, long> components, ref int componentCount)
      {
         // Is this prime already in the Graph?
         if (!components.ContainsKey(prime))
         {
            // Add the new component as its own root.
            componentCount++;
            components.Add(prime, prime);
            return prime;
         }

         // Find the root of this component, keeping track of intermediate
         // ancestors.
         List<long> ancestors = new();
         long r = prime;
         while (components[r] != r)
         {
            ancestors.Add(r);
            r = components[r];
         }

         // Update all intermediate ancestors to point to the root.
         foreach (long p in ancestors)
            components[p] = r;

         return r;
      }

      /// <summary>
      /// Merges two components into a single component
      /// </summary>
      /// <param name="p1">A prime representing the first component.</param>
      /// <param name="p2">A prime representing the second component.</param>
      /// <param name="components"></param>
      /// <param name="componentCount"></param>
      private static void Union(long p1, long p2, Dictionary<long, long> components, ref int componentCount)
      {
         long r1 = Find(p1, components, ref componentCount);
         long r2 = Find(p2, components, ref componentCount);

         // Are they already part of the same component?
         if (r1 == r2)
            return;

         // Smaller primes occur more often than larger ones, so join the larger
         // prime to the smaller primes component.  This will usually join the
         // smaller component to the larger.  See Ref C, section 2 (Counting
         // Fundamental Cycles)
         if (p2 < p1)
         {
            long tmp = p2;
            p2 = p1;
            p1 = tmp;
            tmp = r2;
            r2 = r1;
            r1 = tmp;
         }

         // Merge the r2 component into the r1 component.
         components[r2] = r1;
         componentCount--;
      }
      #endregion

      /// <summary>
      /// Instances of this class contain the two records referred to in
      /// Ref. D as being the entries in the Primes By Relations table.
      /// </summary>
      private class TwoRecords : IEnumerable<TPRelation>
      {
         private readonly HashSet<long> _unmatchedPrimes = new();
         private readonly List<TPRelation> _chain = new();

         public TwoRecords(TPRelation firstRelation)
         {
            _chain.Add(firstRelation);
            foreach (long p in firstRelation)
               _unmatchedPrimes.Add(p);
         }

         public bool ContainsPrime(long p)
         {
            return _unmatchedPrimes.Contains(p);
         }

         /// <summary>
         /// Determines if adding the given relation will reduce the number
         /// of unmatched primes in this chain.
         /// </summary>
         /// <param name="relation"></param>
         /// <returns></returns>
         public bool WillReduceUnmatched(TPRelation relation)
         {
            int c = 0;
            foreach (long p in relation)
               if (_unmatchedPrimes.Contains(p))
                  c++;
            return relation.Count - c < c;
         }

         public int UnmatchedPrimeCount { get => _unmatchedPrimes.Count; }

         /// <summary>
         /// 
         /// </summary>
         /// <returns></returns>
         /// <remarks>
         /// <para>
         /// It is an error to call this method if UnmatchedPrimeCount == 0.
         /// If UnmatchedPrimeCount > 1, which prime is returned is undefined.
         /// </para>
         /// </remarks>
         public long GetUnmatchedPrime()
         {
            return _unmatchedPrimes.First();
         }

         public int ChainCount { get => _chain.Count; }

         public void Combine(TwoRecords other)
         {
            // For a prime to remain in the HashSet, it must occur in only
            // one of this._unmatchedPrimes and other._unmatchedPrimes
            foreach (long p in other._unmatchedPrimes)
               if (_unmatchedPrimes.Contains(p))
                  _unmatchedPrimes.Remove(p);
               else
                  _unmatchedPrimes.Add(p);

            _chain.AddRange(other._chain);
         }

         public void Combine(TPRelation singleton)
         {
            // For a prime to remain in the HashSet, it must occur in only
            // one of this._unmatchedPrimes and other._unmatchedPrimes
            long p = singleton[0];
            if (_unmatchedPrimes.Contains(p))
               _unmatchedPrimes.Remove(p);
            else
               _unmatchedPrimes.Add(p);

            _chain.Add(singleton);
         }

         public static Relation GetRelation(TwoRecords tr0, TwoRecords tr1)
         {
            BigInteger qOfX = BigInteger.One;
            BigInteger x = BigInteger.One;
            BigBitArray exponentVector = new BigBitArray(tr0._chain[0].ExponentVector.Capacity);
            RelationOrigin origin = RelationOrigin.FullyFactored;

            foreach(TPRelation j in tr0._chain)
            {
               qOfX *= j.QOfX;
               x *= j.X;
               exponentVector.Xor(j.ExponentVector);
               origin = (RelationOrigin)Math.Max((int)origin, (int)j.Origin);
            }

            foreach (TPRelation j in tr1._chain)
            {
               qOfX *= j.QOfX;
               x *= j.X;
               exponentVector.Xor(j.ExponentVector);
               origin = (RelationOrigin)Math.Max((int)origin, (int)j.Origin);
            }

            return new Relation(qOfX, x, exponentVector, origin);
         }

         public static Relation GetRelation(TwoRecords tr0, TPRelation singleton)
         {
            BigInteger qOfX = BigInteger.One;
            BigInteger x = BigInteger.One;
            BigBitArray exponentVector = new BigBitArray(tr0._chain[0].ExponentVector.Capacity);
            RelationOrigin origin = RelationOrigin.FullyFactored;

            foreach (TPRelation j in tr0._chain)
            {
               qOfX *= j.QOfX;
               x *= j.X;
               exponentVector.Xor(j.ExponentVector);
               origin = (RelationOrigin)Math.Max((int)origin, (int)j.Origin);
            }

            qOfX *= singleton.QOfX;
            x *= singleton.X;
            exponentVector.Xor(singleton.ExponentVector);
            origin = (RelationOrigin)Math.Max((int)origin, (int)singleton.Origin);

            return new Relation(qOfX, x, exponentVector, origin);
         }

         public IEnumerator<TPRelation> GetEnumerator()
         {
            return _chain.GetEnumerator();
         }

         IEnumerator IEnumerable.GetEnumerator()
         {
            return _chain.GetEnumerator();
         }
      }

      /// <inheritdoc />
      public Relation this[int index] { get => _relations[index]; }


      /// <inheritdoc />
      public int Count
      {
         get
         {
            // Per Ref. D, this formula is VERY rarely wrong, but close
            // enough for practical use.
            // C + A - P - 2 * R
            // where:
            //   C: number of components
            //   A: number of edges
            //   P: number of primes (Nodes in the Graph)
            //   R: Number of Relations (This appears to refer to Partial Relations)
            //
            // In Ref. D, they've added three edges per relation (A), then subtracted
            // twice the number of relations.  This is peculiar as it implies
            // that A = 3 * R, and the given formula simplifies to
            //   C + R - P
            // just as in the Two Large Primes case.

            // Is it time to count again?
            int partialsCount;
            lock (_lockPartials)
               partialsCount = _singletons.Count + _doubles.Count + _triples.Count;
            if (partialsCount > _nextCountThreshold)
            {
               _taskCount = Task.Run(CountCycles);
               _taskCount.ContinueWith((task) =>
               {
                  _taskCount?.Wait();   // bizarre, but we get intermittent
                                        // InvalidOperation exceptions from
                                        // Dispose due to _taskCount not being
                                        // completed!
                  _taskCount?.Dispose();
                  _taskCount = null;
               });
            }

            int rv;
            lock (_lockRelations)
               rv = _relations.Count;
            lock (_lockCount)
               rv += _count;

            return rv;
         }
      }

      /// <inheritdoc />
      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         // This may result in slightly more Relations than counted when
         // finishing Sieving.
         StopBackgroundTasks();

         CombineRelations();

         IMatrix rv = matrixFactory.GetMatrix(_factorBaseSize, _relations.Count);
         for (int col = 0; col < _relations.Count; col++)
         {
            Relation rel = _relations[col];
            for (int row = 0; row < _factorBaseSize; row++)
               if (rel.ExponentVector[row])
                  rv[row, col] = true;
         }

         return rv;
      }

      /// <summary>
      /// Combines as many as possible of the Partial (1-, 2-, and 3-Large Prime)
      /// Relations as possible into full Relations.
      /// </summary>
      /// <remarks>
      /// <para>
      /// This method implements the combining of 1-, 2-, and 3-Large Primes
      /// Relations into full Relations.  It is based on the computing cycles
      /// portion of Section 2.3 of Ref. D.
      /// </para>
      /// <para>
      /// Note that Relations of the form (1, p, p) were combined into full
      /// Relations during Sieving, and not entered into the lists.
      /// </para>
      /// <para>
      /// For Relations of the form (1, 1, p), the first was kept in the _singletons.
      /// Subsequent matching Relations were combined and entered into _relations.
      /// </para>
      /// </remarks>
      private void CombineRelations()
      {
         Dictionary<long, List<TPRelation>> relationsByPrimes;
         Dictionary<TPRelation, TwoRecords> primesByRelation;
         BuildHashTables(out relationsByPrimes, out primesByRelation);

         // We only need to make one pass through the Singletons table as
         // all possible matches with them will be made in one pass.
         foreach (TPRelation singleton in _singletons)
            CombineOneSingleton(singleton, relationsByPrimes, primesByRelation);

         bool noChanges = false;
         while (!noChanges)
         {
            noChanges = true;

            // linear sweep through pbr table
            foreach (TPRelation r0 in primesByRelation.Keys)
               if (CombineOneRelation(r0, relationsByPrimes, primesByRelation))
                  noChanges = false;
         }
      }

      private void BuildHashTables(out Dictionary<long, List<TPRelation>> relationsByPrimes,
         out Dictionary<TPRelation, TwoRecords> primesByRelation)
      {
         int sz = 5 * (_doubles.Count + _triples.Count) / 4;
         relationsByPrimes = new(sz);
         primesByRelation = new(sz);

         AddRelationsToDictionaries(_doubles, relationsByPrimes, primesByRelation);
         AddRelationsToDictionaries(_triples, relationsByPrimes, primesByRelation);
      }

      private void AddRelationsToDictionaries(List<TPRelation> relations,
         Dictionary<long, List<TPRelation>> relationsByPrimes,
         Dictionary<TPRelation, TwoRecords> primesByRelation)
      {
         foreach(TPRelation relation in relations)
         {
            foreach (long p in relation)
            {
               List<TPRelation>? lstRelations;
               if (!relationsByPrimes.TryGetValue(p, out lstRelations))
               {
                  lstRelations = new();
                  relationsByPrimes.Add(p, lstRelations);
               }
               lstRelations.Add(relation);
            }

            primesByRelation.Add(relation, new TwoRecords(relation));
         }
      }

      /// <summary>
      /// Combines one "par" into the other relations, if possible.
      /// </summary>
      /// <param name="r0"></param>
      /// <returns>true if changes were made; false otherwise.</returns>
      private bool CombineOneRelation(TPRelation r0,
         Dictionary<long, List<TPRelation>> relationsByPrimes,
         Dictionary<TPRelation, TwoRecords> primesByRelation)
      {
         bool rv = false;
         List<TPRelation> removeFromList = new();
         TwoRecords tr0 = primesByRelation[r0];

         // Ref. D contains the phrase "If the relation r0 is a 'par'".
         // Here this is interpreted not as the Relation r0, but rather as
         // if the associated chain has exactly 1 unmatched prime.
         if (tr0.UnmatchedPrimeCount != 1)
            return false;

         long unmatchedPrime0 = tr0.GetUnmatchedPrime();
         foreach (TPRelation ri in relationsByPrimes[unmatchedPrime0])
         {
            // We must not try to combine r0 with itself.
            if (ri == r0)
               continue;

            // Here, we interpret the phrase "if the relation ri is a 'par'"
            // as "if the chain associated with ri has an unmatched prime count of 1".
            TwoRecords tri = primesByRelation[ri];
            if (tri.UnmatchedPrimeCount == 1)
            {
               long unmatchedPrime1 = tri.GetUnmatchedPrime();
               if (unmatchedPrime0 == unmatchedPrime1)
               {
                  // r0 and ri form a cycle: emit a full Relation.
                  rv = true;
                  _relations.Add(TwoRecords.GetRelation(tr0, tri));
                  removeFromList.Add(ri);
               }
            }
            else
            {  // ri has more than one prime.
               // Does one of them match unmatchedPrime0?
               if (tri.ContainsPrime(unmatchedPrime0))
               {
                  // Combine r0 with ri
                  rv = true;
                  tri.Combine(tr0);
                  removeFromList.Add(ri);
               }
            }
         }

         // Mark r0 for deletion from primes-by-relations
         primesByRelation.Remove(r0);

         // Delete r0 from the list keyed by unmatchedPrime0
         relationsByPrimes[unmatchedPrime0].Remove(r0);

         // Remove any ri that were combined with r0
         foreach (TPRelation remove in removeFromList)
            relationsByPrimes[unmatchedPrime0].Remove(remove);

         return rv;
      }

      /// <summary>
      /// Combines one Singleton into the Main Graph, if possible.
      /// </summary>
      /// <param name="r0"></param>
      /// <returns>true if changes were made; false otherwise.</returns>
      private bool CombineOneSingleton(TPRelation r0,
         Dictionary<long, List<TPRelation>> relationsByPrimes,
         Dictionary<TPRelation, TwoRecords> primesByRelation)
      {
         bool rv = false;
         List<TPRelation> removeFromList = new();
         List<TPRelation>? relations;
         long unmatchedPrime0 = r0[0];

         if (!relationsByPrimes.TryGetValue(unmatchedPrime0, out relations))
            return false;

         foreach (TPRelation ri in relations)
         {
            // Here, we interpret the phrase "if the relation ri is a 'par'"
            // as "if the chain associated with ri has an unmatched prime count of 1".
            TwoRecords tri = primesByRelation[ri];
            if (tri.UnmatchedPrimeCount == 1)
            {
               long unmatchedPrime1 = tri.GetUnmatchedPrime();
               if (unmatchedPrime0 == unmatchedPrime1)
               {
                  // r0 and ri form a cycle: emit a full Relation.
                  rv = true;
                  Relation newRelation = TwoRecords.GetRelation(tri, r0);
                  lock(_lockRelations)
                     _relations.Add(newRelation);
                  removeFromList.Add(ri);
               }
            }
            else
            {  // ri has more than one prime.
               // Does one of them match unmatchedPrime0?
               if (tri.ContainsPrime(unmatchedPrime0))
               {
                  // Combine r0 with ri
                  rv = true;
                  tri.Combine(r0);
                  removeFromList.Add(ri);
               }
            }
         }

         // Remove any ri that were combined with r0
         foreach (TPRelation remove in removeFromList)
            relations.Remove(remove);

         return rv;
      }

      /// <inheritdoc />
      public Statistic[] GetStats()
      {
         int[] counts = new int[4];
         foreach (Relation j in _relations)
            counts[(int)j.Origin]++;

         List<Statistic> rv = new();
         rv.Add(new Statistic(StatisticNames.FullyFactored, counts[0]));
         rv.Add(new Statistic(StatisticNames.OneLargePrime, counts[1]));
         rv.Add(new Statistic(StatisticNames.TwoLargePrimes, counts[2]));
         rv.Add(new Statistic(StatisticNames.ThreeLargePrimes, counts[3]));
         rv.Add(new Statistic("Components", _componentCount));
         rv.Add(new Statistic("Edges", _edgeCount));

         return rv.ToArray();
      }

      /// <inheritdoc />
      public void RemoveRelationAt(int index)
      {
         _relations.RemoveAt(index);
      }

      /// <inheritdoc />
      public void PrepareToResieve()
      {
         StartBackgroundTasks();
      }

      /// <inheritdoc />
      public void TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector, BigInteger residual)
      {
         FactorRelation(QofX, x, exponentVector, residual);
      }
   }
}

