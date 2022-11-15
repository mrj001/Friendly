using System;
using System.Collections.Generic;
using System.Numerics;
using Friendly.Library;

//====================================================================
// References
//====================================================================
//
// A. Kefa Rabah , 2006. Review of Methods for Integer Factorization
//    Applied to Cryptography. Journal of Applied Sciences, 6: 458-481.
//    https://scialert.net/fulltext/?doi=jas.2006.458.481
//
// B. Robert D. Silverman, The Multiple Polynomial Quadratic Sieve,
//    Mathematics of Computation, Volume 48, Number 177, January 1987,
//    pages 329-339.
//

namespace Friendly.Library.QuadraticSieve
{
   public class QuadraticSieve : INotifyProgress
   {
      private readonly IParameters _parameters;

      /// <summary>
      /// The original number being factored.
      /// </summary>
      private readonly BigInteger _nOrig;

      /// <summary>
      /// A small prime which is multiplied by the number being factored to
      /// obtain a factor base which is richer in small primes.
      /// </summary>
      private long _multiplier;

      /// <summary>
      /// _nOrig *_multiplier
      /// </summary>
      private BigInteger _n;

      /// <summary>
      /// The Sieve Interval.
      /// </summary>
      private int _M;

      /// <summary>
      /// Ceiling(sqrt(_n))
      /// </summary>
      private BigInteger _rootN;

      private FactorBase _factorBase;

      private Relations _relations;

      private IMatrix _matrix;
      private IMatrixFactory _matrixFactory;

      /// <summary>
      /// A count of the number of completed iterations of sieving.
      /// </summary>
      private int _sieveIntervals;

      private IEnumerator<Polynomial> _polynomials;
      private int _totalPolynomials;

      public event EventHandler<NotifyProgressEventArgs> Progress;

      /// <summary>
      /// Initializes an instance of the Quadratic Sieve algorithm.
      /// </summary>
      /// <param name="parameters">An IParameters instance to supply the
      /// algorithm's parameters.</param>
      /// <param name="n">The number to be factored by this Quadratic Sieve.</param>
      public QuadraticSieve(IParameters parameters, BigInteger n)
      {
         _parameters = parameters;
         _nOrig = n;
         _multiplier = 1;
         _n = n;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.

         _factorBase = null;
         _relations = null;
         _matrix = null;
         _matrixFactory = new MatrixFactory();

         _sieveIntervals = 0;
         _polynomials = null;
         _totalPolynomials = 0;
      }

      protected void OnNotifyProgress(string message)
      {
         Progress?.Invoke(this, new NotifyProgressEventArgs(message));
      }

      public int TotalBSmoothValuesFound { get => _relations.TotalRelationsFound; }

      public int TotalPolynomials { get => _totalPolynomials; }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public int[] GetRelationsStats()
      {
         return _relations.GetStats();
      }

      /// <summary>
      /// Factors the given number into two factors.
      /// </summary>
      /// <param name="n">The number to factor.</param>
      /// <returns>A tuple containing two factors.</returns>
      /// <remarks>
      /// <para>
      /// Pre-conditions:
      /// <list type="number">
      /// <item>Small prime factors have already been factored out.</item>
      /// <item>The given number, n, is not a power.</item>
      /// <item>n is not a prime number.</item>
      /// </list>
      /// </para>
      /// </remarks>
      public (BigInteger, BigInteger) Factor()
      {
         FindFactorBase();
         OnNotifyProgress($"The Factor Base contains {_factorBase.Count} primes.  Maximum prime: {_factorBase[_factorBase.Count - 1]}");

         _relations = new Relations(_factorBase.Count);

         _M = _parameters.FindSieveInterval(_n);
         _polynomials = (new MultiPolynomial(_n, _rootN, _factorBase.MaxPrime, _M)).GetEnumerator();

         FindBSmooth();

         int retryCount = 0;
         int retryLimit = 100;
         int nullVectorsChecked = 0;
         while (retryCount < retryLimit)
         {
            OnNotifyProgress($"Have {_relations.RelationCount} relations; reducing Matrix");
            _matrix = _relations.GetMatrix(_matrixFactory);
            _matrix.Reduce();
            List<BigBitArray> nullVectors = _matrix.FindNullVectors();
            OnNotifyProgress($"Found {nullVectors.Count} Null Vectors");

            BigInteger x, y;
            foreach (BigBitArray nullVector in nullVectors)
            {
               nullVectorsChecked++;
               x = BigInteger.One;
               y = BigInteger.One;
               for (int j = 0, jul = _matrix.Columns; j < jul; j++)
               {
                  if (nullVector[j])
                  {
                     Relation relation = _relations[j];
                     x *= relation.X;
                     y *= relation.QOfX;
                  }
               }
               BigInteger t = BigIntegerCalculator.SquareRoot(y);
               Assertions.True(t * t == y);  // y was constructed to be a square.
               y = t;

               x %= _n;
               if (x < 0)
                  x += _n;
               y %= _n;

               // Is x = +/-y mod n?
               if (x == y || x + y == _n)
                  continue;

               BigInteger xmy = x - y;
               if (xmy < 0)
                  xmy += _n;

               BigInteger f1 = BigIntegerCalculator.GCD(_n, xmy);
               if (f1 != 1 && f1 != _n && f1 != _nOrig && f1 != _multiplier)
               {
                  BigInteger q = BigInteger.DivRem(f1, _multiplier, out BigInteger remainder);
                  if (remainder == 0)
                     f1 = q;
                  OnNotifyProgress($"Factored after checking {nullVectorsChecked} Null Vectors.");
                  return (f1, _nOrig / f1);
               }
            }

            retryCount++;
            OnNotifyProgress($"Retry number: {retryCount}");
            PrepareToResieve();
            FindBSmooth();
         }

         // We've gone back and retried 100 times and run out of squares each
         // time. This is cause for suspicion.
         throw new ApplicationException($"Ran out of squares while factoring {_n:N0}\nTotal B-Smooth Values Found: {_relations.TotalRelationsFound}\nFactor Base Count: {_factorBase.Count}");
      }

      /// <summary>
      /// Determines an appropriate factor base for factoring the given number
      /// </summary>
      private void FindFactorBase()
      {
         _factorBase = FactorBaseCandidate.GetFactorBase(_parameters, _nOrig);
         _multiplier = _factorBase.Multiplier;
         _n = _multiplier * _nOrig;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.
      }

      /// <summary>
      /// Sieves for a list of B-Smooth numbers
      /// </summary>
      /// <remarks>
      /// <para>
      /// Each column of the Matrix contains the Exponent Vector for the B-Smooth number at
      /// the same index.
      /// </para>
      /// </remarks>
      private void FindBSmooth()
      {
         int fbSize = _factorBase.Count;
         int sieveSize = 2 * _M + 1;
         ushort[] sieve = new ushort[sieveSize];

         double T = _parameters.FindLargePrimeTolerance(_n);
         long pmax = _factorBase[_factorBase.Count - 1].Prime;
         double pmaxt = Math.Pow(pmax, T);

         int smallPrimeLimit = _parameters.FindSmallPrimeLimit(_n);
         float smallPrimeLog = 0;
         int firstPrimeIndex = 2;
         while (_factorBase[firstPrimeIndex].Prime <= smallPrimeLimit)
         {
            smallPrimeLog += _factorBase[firstPrimeIndex].Log / _factorBase[firstPrimeIndex].Prime;
            firstPrimeIndex++;
         }

         int numRelationsNeeded = fbSize + 1;

         do
         {
            if (!_polynomials.MoveNext())
               throw new ApplicationException($"Ran out of polynomials while factoring {_n:N0}\nTotal B-Smooth Values Found: {_relations.TotalRelationsFound}\nFactor Base Count: {_factorBase.Count}");

            Polynomial poly = _polynomials.Current;
            _totalPolynomials++;

            // Initialize the Sieve array to zero over the range [-M, M]
            for (int j = 0; j < sieveSize; j++)
               sieve[j] = 0;

            // The Sieve Threshold is per Ref. B, Section 4 (iii),
            // adjusted for the exclusion of small primes from sieving.
            ushort sieveThreshold = (ushort)Math.Round(Math.Log(_M * Math.Sqrt((double)_n / 2) / pmaxt) - smallPrimeLog);

            // for all primes in the factor base (other than -1 and 2) add Add log(p) to the sieve
            for (int j = firstPrimeIndex, jul = _factorBase.Count; j < jul; j ++)
            {
               FactorBasePrime prime = _factorBase[j];
               ushort log = (ushort)prime.Log;

               // Find the roots of Q(x) mod p.
               int curPrime = prime.Prime;
               int rootnModP = prime.RootNModP;
               BigInteger inv2a = BigIntegerCalculator.FindInverse(2 * poly.A, curPrime);  // BUG 2 is not invertible
               int x1 = (int)((-poly.B + rootnModP) * inv2a % curPrime);
               int x2 = (int)((-poly.B - rootnModP) * inv2a % curPrime);

               // Translate to the first index of Q and exponentVectors where
               // the values will divide evenly
               int offset = (int)(_M % curPrime);
               int index1 = x1 + offset;
               if (index1 < 0) index1 += curPrime;
               if (index1 >= curPrime) index1 -= curPrime;
               int index2 = x2 + offset;
               if (index2 < 0) index2 += curPrime;
               if (index2 >= curPrime) index2 -= curPrime;

               // Add the Log of the Prime to each r +/- p location.
               AddLogs(sieve, index1, curPrime, log);
               AddLogs(sieve, index2, curPrime, log);
            }

            // Find all sieve locations which exceed the Sieve Threshold
            for (int x = -_M, idx = 0; x <= _M; x ++, idx++)
            {
               if (sieve[idx] > sieveThreshold)
               {
                  BigBitArray exponentVector = new BigBitArray(_factorBase.Count);
                  BigInteger Q = poly.Evaluate(x);
                  BigInteger origQ = Q;

                  // Handle the -1 prime
                  if (Q < 0)
                  {
                     Q *= -1;
                     exponentVector.FlipBit(0);
                  }

                  // Handle the remaining primes
                  for (int j = 1, jul = _factorBase.Count; j < jul; j ++)
                  {
                     int curPrime = _factorBase[j].Prime;
                     BigInteger q, r;
                     q = BigInteger.DivRem(Q, curPrime, out r);
                     while (r == 0)
                     {
                        Q = q;
                        exponentVector.FlipBit(j);
                        q = BigInteger.DivRem(Q, curPrime, out r);
                     }
                  }

                  // Is the number B-Smooth?
                  if (Q == 1)
                     _relations.AddRelation(new Relation(origQ, poly.EvaluateLHS(x), exponentVector));
                  else if (Q < (long)pmaxt && Q > _factorBase[_factorBase.Count - 1].Prime)
                     _relations.AddPartialRelation(new PartialRelation(origQ, poly.EvaluateLHS(x), exponentVector, (long)Q));
               }
            }

            _sieveIntervals ++;

         } while (_relations.RelationCount < numRelationsNeeded);
      }

      private static void AddLogs(ushort[] sieve, int startIndex, int stride, ushort log)
      {
         for (int j = startIndex; j < sieve.Length; j += stride)
            sieve[j] += log;
      }

      /// <summary>
      /// Prepares for another round of Sieving
      /// </summary>
      /// <remarks>
      /// <para>
      /// Call this after the set of null vectors has failed to produce a
      /// factorization.  The set of free variables is discarded, and the
      /// Exponent Vector Matrix is recalculated.
      /// </para>
      /// </remarks>
      public void PrepareToResieve()
      {
         if (!_polynomials.MoveNext())
            throw new ApplicationException("Ran out of polynomials");
         _totalPolynomials++;

         // Remove the free columns which generated the non-useful null vectors.
         List<int> freeColumns = _matrix.FindFreeColumns();
         _matrix = null;
         for (int j = freeColumns.Count - 1; j >= 0; j--)
            _relations.RemoveRelationAt(j);
      }
   }
}
