#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Friendly.Library.Pollard;

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
      /// This is the "Factoring Queue"
      /// </summary>
      /// <remarks>
      /// <para>
      /// Incoming Relations from the Sieve are placed into this Queue.  When
      /// removed, the residuals are factored and and appropriate TPRelation
      /// object is constructed, and placed in the next Queue.
      /// </para>
      /// </remarks>
      private readonly BlockingCollection<RelationQueueItem> _queueFactor;
      private readonly Task _taskFactor;
      private bool _completeTaskFactor;
      private int _maxFactorQueueLength;

      /// <summary>
      /// This is the "Builder Queue"
      /// </summary>
      /// <remarks>
      /// <para>
      /// TPRelation objects are removed from this queue and inserted into the
      /// data structures used to find cycles and build full Relations.
      /// </para>
      /// </remarks>
      private readonly BlockingCollection<TPRelation> _queueBuilder;
      private readonly Task _taskBuilder;
      private bool _completeTaskBuilder;
      private int _maxBuilderQueueLength;

      /// <summary>
      /// This stores lists of TPRelation objects keyed by one of the primes
      /// contained in it.  Each TPRelation is entered once for each prime it
      /// contains.
      /// </summary>
      private Dictionary<long, List<TPRelation>> _relationsByPrimes;

      private Dictionary<TPRelation, TwoRecords> _primesByRelation;

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

         _queueBuilder = new BlockingCollection<TPRelation>();
         _completeTaskBuilder = false;
         _maxBuilderQueueLength = 0;
         _taskBuilder = Task.Run(DoBuilderTask);

         int initialCapacity = 1 << 18;
         _relationsByPrimes = new Dictionary<long, List<TPRelation>>(initialCapacity);
         _primesByRelation = new Dictionary<TPRelation, TwoRecords>(initialCapacity);
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
            _relations.Add(new Relation(item.QofX, item.X, item.ExponentVector));
         }
         else if (item.Residual < _maxLargePrime && item.Residual > _maxFactor)
         {
            TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
               new long[] { (long)item.Residual });
            _queueBuilder.Add(relation);
         }
         else if (item.Residual < _maxTwoPrimes && !Primes.IsPrime(item.Residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger p1, BigInteger p2) = rho.Factor(item.Residual);
            if (p1 != p2)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)p1, (long)p2 });
               _queueBuilder.Add(relation);
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
               _queueBuilder.Add(relation);
            }
            else if (f1 == f3)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f2 }, RelationOrigin.ThreeLargePrimes);
               _queueBuilder.Add(relation);
            }
            else if (f2 == f3)
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f1 }, RelationOrigin.ThreeLargePrimes);
               _queueBuilder.Add(relation);
            }
            else
            {
               TPRelation relation = new TPRelation(item.QofX, item.X, item.ExponentVector,
                  new long[] { (long)f1, (long)f2, (long)f3 });
               _queueBuilder.Add(relation);
            }
         }

         _maxBuilderQueueLength = Math.Max(_maxBuilderQueueLength, _queueBuilder.Count);
      }
      #endregion

      #region Building full relations from Partials...
      private void DoBuilderTask()
      {
         TPRelation? relation;

         while (!_completeTaskBuilder)
         {
            if (_queueBuilder.TryTake(out relation, 20))
               BuildRelations(relation);
         }

         // Finish emptying the queue
         while (_queueBuilder.TryTake(out relation, 0))
            BuildRelations(relation);
      }

      private void BuildRelations(TPRelation relation)
      {
         // A duplicate relation is very unlikely, but we'll check anyway
         if (_primesByRelation.ContainsKey(relation))
            return;

         // Enter the new Relation into the Relations-By-Primes Table
         foreach (long p in relation)
         {
            List<TPRelation>? relations;
            if (_relationsByPrimes.TryGetValue(p, out relations))
            {
               // Scan all the Primes-by-Relations in the list to see if this
               // new Relation will help reduce the unmatched primes.
               List<TPRelation> remove = new();
               foreach (TPRelation j in relations)
               {
                  TwoRecords? tr;
                  if (_primesByRelation.TryGetValue(j, out tr) && tr.WillReduceUnmatched(relation))
                  {
                     tr.AddRelation(relation);
                     if (tr.UnmatchedPrimeCount == 0)
                     {
                        remove.Add(j);
                        Relation newRelation = tr.GetRelation();
                        _relations.Add(newRelation);
                     }
                  }
               }
               // Remove any TPRelation objects that got all their primes matched.
               foreach (TPRelation j in remove)
               {
                  relations.Remove(j);
                  _primesByRelation.Remove(j);
               }
            }
            else
            {
               relations = new List<TPRelation>();
               _relationsByPrimes.Add(p, relations);
            }

            // Add the new TPRelation into the list for this prime.
            relations.Add(relation);
         }

         // Add the new Relation into the Primes-By-Relations table
         _primesByRelation.Add(relation, new TwoRecords(relation));
      }

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

         public int ChainCount { get => _chain.Count; }

         public void AddRelation(TPRelation newRelation)
         {
            foreach (long p in newRelation)
               if (_unmatchedPrimes.Contains(p))
                  _unmatchedPrimes.Remove(p);
               else
                  _unmatchedPrimes.Add(p);
            _chain.Add(newRelation);
         }

         public Relation GetRelation()
         {
            BigInteger qOfX = BigInteger.One;
            BigInteger x = BigInteger.One;
            BigBitArray exponentVector = new BigBitArray(_chain[0].ExponentVector.Capacity);
            RelationOrigin origin = RelationOrigin.FullyFactored;

            foreach(TPRelation j in _chain)
            {
               qOfX *= j.QOfX;
               x *= j.X;
               exponentVector.Xor(j.ExponentVector);
               origin = (RelationOrigin)Math.Max((int)origin, (int)j.Origin);
            }

            return new Relation(qOfX, x, exponentVector, origin);
         }
      }
      #endregion

      /// <inheritdoc />
      public Relation this[int index] { get => _relations[index]; }


      /// <inheritdoc />
      public int Count { get => _relations.Count; }

      /// <inheritdoc />
      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         // TODO: it is not necessary to wait for these tasks if there are
         //  sufficient relations already....
         _completeTaskFactor = true;
         _taskFactor.Wait();
         _completeTaskBuilder = true;
         _taskBuilder.Wait();

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

      /// <inheritdoc />
      public Statistic[] GetStats()
      {
         int[] counts = new int[4];
         foreach (Relation j in _relations)
            counts[(int)j.Origin]++;

         List<Statistic> rv = new();
         rv.Add(new Statistic(Statistic.FullyFactored, counts[0]));
         rv.Add(new Statistic(Statistic.OneLargePrime, counts[1]));
         rv.Add(new Statistic(Statistic.TwoLargePrimes, counts[2]));
         rv.Add(new Statistic(Statistic.ThreeLargePrimes, counts[3]));
         rv.Add(new Statistic("RelationsByPrimesLoad", ((float)_relationsByPrimes.Count) / _relationsByPrimes.EnsureCapacity(0)));
         rv.Add(new Statistic("PrimesByRelationLoad", ((float)_primesByRelation.Count) / _primesByRelation.EnsureCapacity(0)));
         rv.Add(new Statistic("MaxFactorQueueLength", _maxFactorQueueLength));
         rv.Add(new Statistic("MaxBuilderQueueLength", _maxBuilderQueueLength));
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
               _taskFactor.Dispose();
               _taskBuilder.Dispose();
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

