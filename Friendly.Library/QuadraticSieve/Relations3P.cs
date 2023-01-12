#nullable enable
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

      //====================================================================
      // BEGIN member data for dataflow blocks
      /// <summary>
      /// This is the "Input Queue"
      /// </summary>
      /// <remarks>
      /// <para>
      /// Incoming Relations from the Sieve are placed into this Queue.
      /// </para>
      /// </remarks>
      private BufferBlock<RelationQueueItem>? _queueInput;
      private int _maxInputQueueLength = 0;

      /// <summary>
      /// This Action Block is responsible for factoring residuals,
      /// creating TPRelation objects, and posting them to the next appropriate
      /// Action Block.
      /// </summary>
      private ActionBlock<RelationQueueItem>? _actionFactor;
      private int _maxFactorQueueLength = 0;

      private ActionBlock<TPRelation>? _actionSingleton;
      private int _maxSingletonQueueLength = 0;

      private ActionBlock<TPRelation>? _actionInsertMainGraph;
      private int _maxInsertQueueLength = 0;

      // END member data for dataflow blocks
      //====================================================================

      //====================================================================
      // BEGIN member data for tracking and promoting singletons
      private readonly List<TPRelation> _singletons;

      private int _singletonRetryIndex = 0;
      // END member data for tracking and promoting singletons.
      //====================================================================

      //====================================================================
      // BEGIN member data for the "Main Graph"
      /// <summary>
      /// This stores lists of TPRelation objects keyed by one of the primes
      /// contained in it.  Each TPRelation is entered once for each prime it
      /// contains.
      /// </summary>
      private Dictionary<long, List<TPRelation>> _relationsByPrimes;

      private Dictionary<TPRelation, TwoRecords> _primesByRelation;
      // END member data for the "Main Graph"
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
      private const string MaxQueueLengthStatName = "maxqueuelength";
      private const string MaxFactorQueueLengthStatName = "maxfactorqueuelength";
      private const string MaxSingletonQueueLengthStatName = "maxsingletonqueuelength";
      private const string MaxInsertQueueLengthStatName = "maxinsertqueuelength";
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

         _maxFactorQueueLength = 0;
         StartFactorTask();

         _edgeCount = 0;
         _componentCount = 0;
         _components = new Dictionary<long, long>(InitialCapacity);
         _singletons = new List<TPRelation>(InitialCapacity);
         _relationsByPrimes = new Dictionary<long, List<TPRelation>>(InitialCapacity);
         _primesByRelation = new Dictionary<TPRelation, TwoRecords>(InitialCapacity);
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

         // Restore stats that we need to round trip
         List<Statistic> statistics = new();
         rdr.ReadStartElement(StatisticsNodeName);
         while (rdr.IsStartElement(StatisticNodeName))
            statistics.Add(new Statistic(rdr));
         rdr.ReadEndElement();
         _maxInputQueueLength = (int)(statistics.Where(s => s.Name == MaxQueueLengthStatName).First().Value);
         _maxFactorQueueLength = (int)(statistics.Where(s => s.Name == MaxFactorQueueLengthStatName).First().Value);
         _maxSingletonQueueLength = (int)(statistics.Where(s => s.Name == MaxSingletonQueueLengthStatName).First().Value);
         _maxInsertQueueLength = (int)(statistics.Where(s => s.Name == MaxInsertQueueLengthStatName).First().Value);

         // Read the full Relations
         _relations = new();
         rdr.ReadStartElement(RelationsNodeName);
         while (rdr.IsStartElement("r"))
            _relations.Add(new Relation(rdr));
         rdr.ReadEndElement();

         // Initialize the Graph
         _edgeCount = 0;
         _componentCount = 0;
         _components = new Dictionary<long, long>(InitialCapacity);
         _singletons = new List<TPRelation>(InitialCapacity);
         _relationsByPrimes = new Dictionary<long, List<TPRelation>>(InitialCapacity);
         _primesByRelation = new Dictionary<TPRelation, TwoRecords>(InitialCapacity);

         // Read the Partial relations (1-, 2-, and 3-prime relations)
         rdr.ReadStartElement(PartialRelationsNodeName);
         while (rdr.IsStartElement("r"))
         {
            TPRelation tpr = new TPRelation(rdr);
            if (tpr.Count == 1)
               AddSingletonRelation(tpr);
            else
               AddPartialRelationToMainGraph(tpr);
         }
         rdr.ReadEndElement();

         CombineRelations();

         StartFactorTask();

         rdr.ReadEndElement();
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // Shutdown the Factoring task so it is safe to serialize this object.
         StopFactorTask();
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
         Statistic statistic = new Statistic(MaxQueueLengthStatName, _maxInputQueueLength);
         statistic.Serialize(writer, StatisticNodeName);
         statistic = new Statistic(MaxFactorQueueLengthStatName, _maxFactorQueueLength);
         statistic.Serialize(writer, StatisticNodeName);
         statistic = new Statistic(MaxSingletonQueueLengthStatName, _maxSingletonQueueLength);
         statistic.Serialize(writer, StatisticNodeName);
         statistic = new Statistic(MaxInsertQueueLengthStatName, _maxInsertQueueLength);
         statistic.Serialize(writer, StatisticNodeName);
         writer.WriteEndElement();

         writer.WriteStartElement(RelationsNodeName);
         foreach(Relation r in _relations)
            r.Serialize(writer, "r");
         writer.WriteEndElement();

         HashSet<TPRelation> uniquePartials = new(2 * _singletons.Count);
         foreach (TPRelation tpr in _singletons)
            uniquePartials.Add(tpr);
         foreach (TwoRecords tr in _primesByRelation.Values)
            foreach (TPRelation tpr in tr)
               if (!uniquePartials.Contains(tpr))
                  uniquePartials.Add(tpr);

         writer.WriteStartElement(PartialRelationsNodeName);
         foreach (TPRelation tpr in uniquePartials)
            tpr.Serialize(writer, "r");
         writer.WriteEndElement();

         writer.WriteEndElement();
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         if (reason == SerializationReason.SaveState)
            StartFactorTask();
      }

      #region Factoring of incoming residuals
      private void StartFactorTask()
      {
         _queueInput = new BufferBlock<RelationQueueItem>();
         _actionFactor = new ActionBlock<RelationQueueItem>(rqi => FactorRelation(rqi));
         _actionSingleton = new ActionBlock<TPRelation>(tpr => AddSingletonRelation(tpr));
         _actionInsertMainGraph = new ActionBlock<TPRelation>(tpr => AddPartialRelationToMainGraph(tpr));

         _queueInput.LinkTo(_actionFactor);
         _queueInput.Completion.ContinueWith(delegate { _actionFactor.Complete(); } );

         _actionFactor.Completion.ContinueWith(delegate { _actionSingleton.Complete(); });

         _actionSingleton.Completion.ContinueWith(delegate { _actionInsertMainGraph.Complete(); });
      }

      private void StopFactorTask()
      {
         _queueInput?.Complete();
         _actionInsertMainGraph?.Completion.Wait();
         _queueInput = null;
         _actionFactor = null;
         _actionSingleton = null;
         _actionInsertMainGraph = null;
      }

      /// <summary>
      /// Factors residuals and creates TPRelation objects.
      /// </summary>
      /// <param name="item"></param>
      /// <exception cref="ApplicationException">The failure of an invariant implies a bug.</exception>
      /// <remarks>
      /// <para>
      /// This method runs in the _actionFactor's Task.
      /// </para>
      /// </remarks>
      private void FactorRelation(RelationQueueItem item)
      {
         int queueLen = _actionFactor!.InputCount;
         _maxFactorQueueLength = Math.Max(queueLen, _maxFactorQueueLength);

         if (item.Residual == BigInteger.One)
         {
            // Add a fully factored Relation.
            Relation newRelation = new Relation(item.QofX, item.X, item.ExponentVector);
            lock (_lockRelations)
               _relations.Add(newRelation);
            return;
         }
         else if (item.Residual < _maxFactor)
         {
            // We occasionally see residual values that equal the multiplier.
            // Discard these.
            return;
         }
         else if (item.Residual <= _maxSinglePrime)
         {
            // If the Single Large Prime is too large, discard it.
            if (item.Residual > _maxLargePrime)
               return;

            TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
               new long[] { (long)item.Residual });
            _actionSingleton!.Post(relation);
            return;
         }
         else if (item.Residual <= _maxTwoPrimes && !Primes.IsPrime(item.Residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger p1, BigInteger p2) = rho.Factor(item.Residual);

            // If either factor is larger than _maxLargePrime, discard.
            if (p1 > _maxLargePrime || p2 > _maxLargePrime)
               return;

            if (p1 != p2)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)p1, (long)p2 });
               _actionInsertMainGraph!.Post(relation);
               return;
            }
            else
            {
               Relation newRelation = new Relation(item.QofX, item.X, item.ExponentVector, RelationOrigin.TwoLargePrimes);
               lock(_lockRelations)
                  _relations.Add(newRelation);
               return;
            }
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
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f3 }, RelationOrigin.ThreeLargePrimes);
               _actionInsertMainGraph!.Post(relation);
               return;
            }
            else if (f1 == f3)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f2 }, RelationOrigin.ThreeLargePrimes);
               _actionInsertMainGraph!.Post(relation);
               return;
            }
            else if (f2 == f3)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f1 }, RelationOrigin.ThreeLargePrimes);
               _actionInsertMainGraph!.Post(relation);
               return;
            }
            else
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f1, (long)f2, (long)f3 });
               _actionInsertMainGraph!.Post(relation);
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
         int queueLen = _actionSingleton?.InputCount ?? 0;
         _maxSingletonQueueLength = Math.Max(queueLen, _maxSingletonQueueLength);

         int index = _singletons.BinarySearch(relation, new CompareSingletons());

         if (index >= 0)
         {
            // The new Singleton matches an existing one, so create a full
            // relation.
            TPRelation otherRelation = _singletons[index];
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
            _singletons.Insert(index, relation);
         }

         CombineOneSingleton(relation);

         // If we don't have very many Singleton relations yet, do not retry
         // inserting them into the main graph.
         if (_singletons.Count < 1000)
            return;

         // Only retry main graph re-insertion so long as no new singleton
         // relations show up.
         int retryCount = 0;
         while ((_actionSingleton?.InputCount ?? int.MaxValue) == 0 && retryCount < 10)
         {
            CombineOneSingleton(_singletons[_singletonRetryIndex]);
            retryCount++;
            _singletonRetryIndex++;
            if (_singletonRetryIndex == _singletons.Count)
               _singletonRetryIndex = 0;
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

      /// <summary>
      /// Adds a Partial Relation to the Main Graph.
      /// </summary>
      /// <param name="relation"></param>
      /// <exception cref="ApplicationException">Violation of an invariant implies a bug.</exception>
      /// <remarks>
      /// <para>
      /// This runs in the _actionInsertMainGraph Action Block.  It is also
      /// called on the main thread during construction when resuming from
      /// saved state.
      /// </para>
      /// </remarks>
      private void AddPartialRelationToMainGraph(TPRelation relation)
      {
         int queueLen = _actionInsertMainGraph?.InputCount ?? 0;
         _maxInsertQueueLength = Math.Max(queueLen, _maxInsertQueueLength);

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

            return _relations.Count; // + _componentCount + _primesByRelation.Count - _relationsByPrimes.Count;
         }
      }

      /// <inheritdoc />
      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         // Finish the Factoring Queue.  This may result in slightly more
         // Relations than counted when finishing Sieving.
         StopFactorTask();

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
         // We only need to make one pass through the Singletons table as
         // all possible matches with them will be made in one pass.
         foreach (TPRelation singleton in _singletons)
            CombineOneSingleton(singleton);

         bool noChanges = false;
         while (!noChanges)
         {
            noChanges = true;

            // linear sweep through pbr table
            foreach (TPRelation r0 in _primesByRelation.Keys)
               if (CombineOneRelation(r0))
                  noChanges = false;
         }
      }

      /// <summary>
      /// Combines one "par" into the other relations, if possible.
      /// </summary>
      /// <param name="r0"></param>
      /// <returns>true if changes were made; false otherwise.</returns>
      private bool CombineOneRelation(TPRelation r0)
      {
         bool rv = false;
         List<TPRelation> removeFromList = new();
         TwoRecords tr0 = _primesByRelation[r0];

         // Ref. D contains the phrase "If the relation r0 is a 'par'".
         // Here this is interpreted not as the Relation r0, but rather as
         // if the associated chain has exactly 1 unmatched prime.
         if (tr0.UnmatchedPrimeCount != 1)
            return false;

         long unmatchedPrime0 = tr0.GetUnmatchedPrime();
         foreach (TPRelation ri in _relationsByPrimes[unmatchedPrime0])
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
         _primesByRelation.Remove(r0);

         // Delete r0 from the list keyed by unmatchedPrime0
         _relationsByPrimes[unmatchedPrime0].Remove(r0);

         // Remove any ri that were combined with r0
         foreach (TPRelation remove in removeFromList)
            _relationsByPrimes[unmatchedPrime0].Remove(remove);

         return rv;
      }

      /// <summary>
      /// Combines one Singleton into the Main Graph, if possible.
      /// </summary>
      /// <param name="r0"></param>
      /// <returns>true if changes were made; false otherwise.</returns>
      private bool CombineOneSingleton(TPRelation r0)
      {
         bool rv = false;
         List<TPRelation> removeFromList = new();
         List<TPRelation>? relations;
         long unmatchedPrime0 = r0[0];

         if (!_relationsByPrimes.TryGetValue(unmatchedPrime0, out relations))
            return false;

         foreach (TPRelation ri in relations)
         {
            // Here, we interpret the phrase "if the relation ri is a 'par'"
            // as "if the chain associated with ri has an unmatched prime count of 1".
            TwoRecords tri = _primesByRelation[ri];
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
         rv.Add(new Statistic("ComponentsLoad", ((float)_componentCount) / _components.EnsureCapacity(0)));
         rv.Add(new Statistic("RelationsByPrimesLoad", ((float)_relationsByPrimes.Count) / _relationsByPrimes.EnsureCapacity(0)));
         rv.Add(new Statistic("PrimesByRelationLoad", ((float)_primesByRelation.Count) / _primesByRelation.EnsureCapacity(0)));
         rv.Add(new Statistic("MaxInputQueueLength", _maxInputQueueLength));
         rv.Add(new Statistic("MaxFactorQueueLength", _maxFactorQueueLength));
         rv.Add(new Statistic("MaxSingletonQueueLength", _maxSingletonQueueLength));
         rv.Add(new Statistic("MaxInsertQueueLength", _maxInsertQueueLength));

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
         StartFactorTask();
      }

      /// <inheritdoc />
      public void TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector, BigInteger residual)
      {
         RelationQueueItem item = new RelationQueueItem(QofX, x, exponentVector, residual);
         _queueInput!.Post(item);
         int queueLen = _queueInput!.Count;
         _maxInputQueueLength = Math.Max(queueLen, _maxInputQueueLength);
      }
   }
}

