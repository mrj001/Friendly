#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml;
using Friendly.Library.Pollard;
using Friendly.Library.Utility;

// NOTE: For references, see the file QuadraticSieve.cs

namespace Friendly.Library.QuadraticSieve
{
   public class Relations3P : IRelations, IDisposable
   {
      private bool _disposedValue;

      /// <summary>
      /// The set of fully factored Relations found during sieving and combining.
      /// </summary>
      private readonly List<Relation> _relations;

      private readonly int _factorBaseSize;
      private readonly int _maxFactor;
      private readonly long _maxLargePrime;

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

      /// <summary>
      /// The current count of components within the Graph.
      /// </summary>
      private int _componentCount;

      private int _edgeCount;

      /// <summary>
      /// This Dictionary maps each prime (the key) to the component containing
      /// it.
      /// </summary>
      /// <remarks>
      /// <para>
      /// A Union-Find algorithm is used to merge components.  The "root"
      /// of the tree is essentially chosen arbitrarily (the first Vertex found
      /// for that component).  The Key is any prime within the Graph.  The
      /// Value is the Vertex (Prime) chosen to be the root of that Component.
      /// </para>
      /// </remarks>
      private readonly Dictionary<long, long> _components;

      /// <summary>
      /// This is the "Factoring Queue"
      /// </summary>
      /// <remarks>
      /// <para>
      /// Incoming Relations from the Sieve are placed into this Queue.  When
      /// removed, the residuals are factored, classified into components
      /// and placed into the _relationsByPrimes and _primesByRelation
      /// Hash Tables.
      /// </para>
      /// </remarks>
      private readonly BlockingCollection<RelationQueueItem> _queueFactor;
      private Task? _taskFactor;
      private bool _completeTaskFactor;
      private int _maxFactorQueueLength;

      /// <summary>
      /// This stores lists of TPRelation objects keyed by one of the primes
      /// contained in it.  Each TPRelation is entered once for each prime it
      /// contains.
      /// </summary>
      private Dictionary<long, List<TPRelation>> _relationsByPrimes;

      private Dictionary<TPRelation, TwoRecords> _primesByRelation;

      private const int InitialCapacity = 1 << 18;
      public const string TypeNodeName = Relations2P.TypeNodeName;
      private const string LargePrimeNodeName = "maxLargePrime";
      private const string TwoLargePrimeNodeName = "maxTwoLargePrimes";
      private const string ThreeLargePrimeNodeName = "maxThreeLargePrimes";
      private const string MaxQueueLengthNodename = "maxqueuelength";
      private const string RelationsNodeName = "relations";
      private const string PartialRelationsNodeName = "partialrelations";

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The value of the largest prime in the Factor Base.</param>
      /// <param name="maxLargePrime">The maximum value of a residual that will be
      /// considered for a Relation with a Single Large Prime.</param>
      public Relations3P(int factorBaseSize, int maxFactor, long maxLargePrime)
      {
         _relations = new();
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;

         _maxLargePrime = maxLargePrime;
         _maxTwoPrimes = _maxLargePrime > int.MaxValue ? long.MaxValue : _maxLargePrime * _maxLargePrime;
         _maxThreePrimes = ((BigInteger)_maxTwoPrimes) * _maxLargePrime;

         _queueFactor = new BlockingCollection<RelationQueueItem>();
         _completeTaskFactor = false;
         _maxFactorQueueLength = 0;
         _taskFactor = Task.Run(DoFactorTask);

         _edgeCount = 0;
         _componentCount = 0;
         _components = new Dictionary<long, long>(InitialCapacity);
         _relationsByPrimes = new Dictionary<long, List<TPRelation>>(InitialCapacity);
         _primesByRelation = new Dictionary<TPRelation, TwoRecords>(InitialCapacity);
      }

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The value of the largest prime in the Factor Base.</param>
      /// <param name="node">The serialized representation to deserialize.</param>
      public Relations3P(int factorBaseSize, int maxFactor, XmlNode node)
      {
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;

         XmlNode? largePrimeNode = node.FirstChild!.NextSibling;
         if (largePrimeNode is null || largePrimeNode.LocalName != LargePrimeNodeName)
            throw new ArgumentException($"Failed to find <{LargePrimeNodeName}>.");
         if (!long.TryParse(largePrimeNode.InnerText, out _maxLargePrime))
            throw new ArgumentException($"Unable to parse '{largePrimeNode.InnerText}' for <{LargePrimeNodeName}>");

         XmlNode? twoLargePrimeNode = largePrimeNode.NextSibling;
         if (twoLargePrimeNode is null || twoLargePrimeNode.LocalName != TwoLargePrimeNodeName)
            throw new ArgumentException($"Failed to find <{TwoLargePrimeNodeName}>.");
         if (!long.TryParse(twoLargePrimeNode.InnerText, out _maxTwoPrimes))
            throw new ArgumentException($"Unable to parse '{twoLargePrimeNode.InnerText}' for <{TwoLargePrimeNodeName}>");

         XmlNode? threeLargePrimeNode = twoLargePrimeNode.NextSibling;
         if (threeLargePrimeNode is null || threeLargePrimeNode.LocalName != ThreeLargePrimeNodeName)
            throw new ArgumentException($"Failed to find <{ThreeLargePrimeNodeName}>.");
         if (!BigInteger.TryParse(threeLargePrimeNode.InnerText, out _maxThreePrimes))
            throw new ArgumentException($"Unable to parse '{threeLargePrimeNode.InnerText}' for <{ThreeLargePrimeNodeName}>.");

         XmlNode? maxQueueNode = threeLargePrimeNode.NextSibling;
         SerializeHelper.ValidateNode(maxQueueNode, MaxQueueLengthNodename);
         _maxFactorQueueLength = SerializeHelper.ParseIntNode(maxQueueNode!);

         // Read the full Relations
         XmlNode? relationsNode = maxQueueNode!.NextSibling;
         if (relationsNode is null || relationsNode.LocalName != RelationsNodeName)
            throw new ArgumentException($"Failed to find <{RelationsNodeName}>.");
         _relations = new();
         XmlNode? relationNode = relationsNode.FirstChild;
         while (relationNode is not null)
         {
            _relations.Add(new Relation(relationNode));
            relationNode = relationNode.NextSibling;
         }

         // Read the Partial relations (1-, 2-, and 3-prime relations)
         _edgeCount = 0;
         _componentCount = 0;
         _components = new Dictionary<long, long>(InitialCapacity);
         _relationsByPrimes = new Dictionary<long, List<TPRelation>>(InitialCapacity);
         _primesByRelation = new Dictionary<TPRelation, TwoRecords>(InitialCapacity);

         XmlNode? tprelationsNode = relationsNode.NextSibling;
         if (tprelationsNode is null || tprelationsNode.LocalName != PartialRelationsNodeName)
            throw new ArgumentException($"Failed to find <{PartialRelationsNodeName}>.");
         XmlNode? tprNode = tprelationsNode.FirstChild;
         while (tprNode is not null)
         {
            TPRelation tpr = new TPRelation(tprNode);
            AddPartialRelation2(tpr);
            tprNode = tprNode.NextSibling;
         }

         _queueFactor = new BlockingCollection<RelationQueueItem>();
         _completeTaskFactor = false;
         _maxFactorQueueLength = 0;
         _taskFactor = Task.Run(DoFactorTask);
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // Shutdown the Factoring task so it is safe to serialize this object.
         _completeTaskFactor = true;
         _taskFactor?.Wait();
         _taskFactor?.Dispose();
         _taskFactor = null;
      }

      /// <inheritdoc />
      public XmlNode Serialize(XmlDocument doc, string name)
      {
         XmlNode rv = doc.CreateElement(name);

         XmlNode typeNode = doc.CreateElement(TypeNodeName);
         typeNode.InnerText = "Relations3P";
         rv.AppendChild(typeNode);

         SerializeHelper.AddLongNode(doc, rv, LargePrimeNodeName, _maxLargePrime);
         SerializeHelper.AddLongNode(doc, rv, TwoLargePrimeNodeName, _maxTwoPrimes);
         SerializeHelper.AddBigIntegerNode(doc, rv, ThreeLargePrimeNodeName, _maxThreePrimes);
         SerializeHelper.AddIntNode(doc, rv, MaxQueueLengthNodename, _maxFactorQueueLength);

         XmlNode relationsNode = doc.CreateElement(RelationsNodeName);
         rv.AppendChild(relationsNode);
         foreach(Relation r in _relations)
            relationsNode.AppendChild(r.Serialize(doc, "r"));

         XmlNode partialRelationsNode = doc.CreateElement(PartialRelationsNodeName);
         rv.AppendChild(partialRelationsNode);
         foreach (TPRelation tpr in _primesByRelation.Keys)
            partialRelationsNode.AppendChild(tpr.Serialize(doc, "r"));

         return rv;
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         if (reason == SerializationReason.SaveState)
         {
            _completeTaskFactor = false;
            _taskFactor = Task.Run(DoFactorTask);
         }
      }

      #region Factoring of incoming residuals
      private void DoFactorTask()
      {
         RelationQueueItem? queueItem;

         while (!_completeTaskFactor)
         {
            if (_queueFactor.TryTake(out queueItem, 20))
               FactorRelation(queueItem);
         }

         // Finish emptying the queue.
         while (_queueFactor.TryTake(out queueItem, 0))
            FactorRelation(queueItem);
      }

      private void FactorRelation(RelationQueueItem item)
      {
         if (item.Residual == BigInteger.One)
         {
            // Add a fully factored Relation.
            _relations.Add(new Relation(item.QofX, item.X, item.ExponentVector));
         }
         else if (item.Residual < _maxLargePrime && item.Residual > _maxFactor)
         {
            TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
               new long[] { (long)item.Residual });
            AddPartialRelation(relation);
         }
         else if (item.Residual < _maxTwoPrimes && !Primes.IsPrime(item.Residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger p1, BigInteger p2) = rho.Factor(item.Residual);
            if (p1 != p2)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)p1, (long)p2 });
               AddPartialRelation(relation);
            }
            else
               _relations.Add(new Relation(item.QofX, item.X, item.ExponentVector, RelationOrigin.TwoLargePrimes));
         }
         else if (item.Residual < _maxThreePrimes && !Primes.IsPrime(item.Residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger f1, BigInteger f2) = rho.Factor(item.Residual);
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
               // than _maxLargePrime
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

            // Check if a prime occurs twice
            if (f1 == f2)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f3 }, RelationOrigin.ThreeLargePrimes);
               AddPartialRelation(relation);
            }
            else if (f1 == f3)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f2 }, RelationOrigin.ThreeLargePrimes);
               AddPartialRelation(relation);
            }
            else if (f2 == f3)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f1 }, RelationOrigin.ThreeLargePrimes);
               AddPartialRelation(relation);
            }
            else
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f1, (long)f2, (long)f3 });
               AddPartialRelation(relation);
            }
         }
      }

      private void AddPartialRelation(TPRelation relation)
      {
         // Check the special case of adding a second copy of a Single Large
         // Prime.
         if (relation.Count == 1 && _relationsByPrimes.ContainsKey(relation[0]))
         {
            TPRelation? otherRelation = _relationsByPrimes[relation[0]].Where(r => r.Count == 1).FirstOrDefault();
            if (otherRelation is not null)
            {
               BigInteger qOfX = relation.QOfX * otherRelation.QOfX;
               BigInteger x = relation.X * otherRelation.X;
               BigBitArray exponentVector = new BigBitArray(relation.ExponentVector);
               exponentVector.Xor(otherRelation.ExponentVector);
               _relations.Add(new Relation(qOfX, x, exponentVector, RelationOrigin.OneLargePrime));
               return;
            }
         }

         AddPartialRelation2(relation);
      }

      private void AddPartialRelation2(TPRelation relation)
      {
         // Add to the Hash tables
         foreach (long p in relation)
         {
            List<TPRelation>? lstRelations;
            if (!_relationsByPrimes.TryGetValue(p, out lstRelations))
            {
               lstRelations = new();
               _relationsByPrimes.Add(p, lstRelations);
            }
            lstRelations.Add(relation);
         }

         _primesByRelation.Add(relation, new TwoRecords(relation));

         // Add the edges, so we can count Components.
         switch(relation.Count)
         {
            case 1:
               Union(1, relation[0]);
               _edgeCount++;
               break;

            case 2:
               Union(1, relation[0]);
               Union(1, relation[1]);
               Union(relation[0], relation[1]);
               _edgeCount += 3;
               break;

            case 3:
               Union(relation[0], relation[1]);
               Union(relation[0], relation[2]);
               Union(relation[1], relation[2]);
               _edgeCount += 3;
               break;

            default:
               throw new ApplicationException();
         }
      }

      /// <summary>
      /// Finds the "root" of the current set (component) of vertices.
      /// </summary>
      /// <param name="prime">The prime to find the root of.</param>
      /// <returns>The prime which is serving as the root of the given prime's component.</returns>
      private long Find(long prime)
      {
         // Is this prime already in the Graph?
         if (!_components.ContainsKey(prime))
         {
            // Add the new component as its own root.
            _componentCount++;
            _components.Add(prime, prime);
            return prime;
         }

         // Find the root of this component, keeping track of intermediate
         // ancestors.
         List<long> ancestors = new();
         long r = prime;
         while (_components[r] != r)
         {
            ancestors.Add(r);
            r = _components[r];
         }

         // Update all intermediate ancestors to point to the root.
         foreach (long p in ancestors)
            _components[p] = r;

         return r;
      }

      /// <summary>
      /// Merges two components into a single component
      /// </summary>
      /// <param name="p1">A prime representing the first component.</param>
      /// <param name="p2">A prime representing the second component.</param>
      private void Union(long p1, long p2)
      {
         long r1 = Find(p1);
         long r2 = Find(p2);

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
         _components[r2] = r1;
         _componentCount--;
      }
      #endregion

      /// <summary>
      /// Instances of this class contain the two records referred to in
      /// Ref. D as being the entries in the Primes By Relations table.
      /// </summary>
      private class TwoRecords
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

            return _relations.Count + _componentCount + _primesByRelation.Count - _relationsByPrimes.Count;
         }
      }

      /// <inheritdoc />
      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         // Finish the Factoring Queue.  This may result in slightly more
         // Relations than counted when finishing Sieving.
         _completeTaskFactor = true;
         _taskFactor?.Wait();

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
      /// Relations during Sieving, and not entered into the Graph.
      /// </para>
      /// <para>
      /// For Relations of the form (1, 1, p), the first was kept in the Graph.
      /// Subsequent matching Relations were combined and entered into _relations.
      /// </para>
      /// <para>
      /// The Hash Tables relations-by-primes and primes-by-relations were
      /// constructed during sieving.
      /// </para>
      /// </remarks>
      private void CombineRelations()
      {
         bool noChanges = false;
         List<TPRelation> removeFromPrimesByRelation = new();
         List<TPRelation> removeFromList = new();
         while (!noChanges)
         {
            noChanges = true;

            // linear sweep through pbr table
            foreach (TPRelation r0 in _primesByRelation.Keys)
            {
               TwoRecords tr0 = _primesByRelation[r0];

               // Ref. D contains the phrase "If the relation r0 is a 'par'".
               // Here this is interpreted not as the Relation r0, but rather as
               // if the associated chain has exactly 1 unmatched prime.
               if (tr0.UnmatchedPrimeCount == 1)
               {
                  long unmatchedPrime0 = tr0.GetUnmatchedPrime();
                  foreach(TPRelation ri in _relationsByPrimes[unmatchedPrime0])
                  {
                     // We must not try to combine r0 with itself.
                     if (ri == r0)
                        continue;

                     // Here, we interpret the phrase "if the relation ri is a 'par'"
                     // as "if the chain associated with ri has an unmatched prime count of 1".
                     TwoRecords tri = _primesByRelation[ri];
                     if (tri.UnmatchedPrimeCount == 1)
                     {
                        long unmatchedPrime1 = tri.GetUnmatchedPrime();
                        if (unmatchedPrime0 == unmatchedPrime1)
                        {
                           // r0 and ri form a cycle: emit a full Relation.
                           noChanges = false;
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
                           noChanges = false;
                           tri.Combine(tr0);
                        }
                     }
                  }
                  // Mark r0 for deletion from primes-by-relations
                  removeFromPrimesByRelation.Add(r0);

                  // Delete r0 from the list keyed by unmatchedPrime0
                  _relationsByPrimes[unmatchedPrime0].Remove(r0);

                  // Remove any ri that were combined with r0
                  foreach (TPRelation remove in removeFromList)
                     _relationsByPrimes[unmatchedPrime0].Remove(remove);
                  removeFromList.Clear();
               }
            }

            // Delete all the used pars from primes-by-relations
            foreach (TPRelation remove in removeFromPrimesByRelation)
               _primesByRelation.Remove(remove);

            removeFromPrimesByRelation.Clear();
         }
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
         rv.Add(new Statistic("ComponentsLoad", ((float)_componentCount) / _components.EnsureCapacity(0)));
         rv.Add(new Statistic("RelationsByPrimesLoad", ((float)_relationsByPrimes.Count) / _relationsByPrimes.EnsureCapacity(0)));
         rv.Add(new Statistic("PrimesByRelationLoad", ((float)_primesByRelation.Count) / _primesByRelation.EnsureCapacity(0)));
         rv.Add(new Statistic("MaxFactorQueueLength", _maxFactorQueueLength));
         return rv.ToArray();
      }

      /// <inheritdoc />
      public void RemoveRelationAt(int index)
      {
         _relations.RemoveAt(index);
      }

      /// <inheritdoc />
      public void TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector, BigInteger residual)
      {
         RelationQueueItem item = new RelationQueueItem(QofX, x, exponentVector, residual);
         _queueFactor.Add(item);
         int queueLen = _queueFactor.Count;
         _maxFactorQueueLength = Math.Max(queueLen, _maxFactorQueueLength);
      }

      #region IDisposable
      protected virtual void Dispose(bool disposing)
      {
         if (!_disposedValue)
         {
            if (disposing)
            {
               _taskFactor?.Dispose();
               _taskFactor = null;
            }

            _primesByRelation = null!;
            _relationsByPrimes = null!;

            _disposedValue = true;
         }
      }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
      #endregion
   }
}

