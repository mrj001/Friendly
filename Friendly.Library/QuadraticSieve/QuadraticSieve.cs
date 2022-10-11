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

      private List<long> _factorBase;

      private readonly List<BigInteger> _xValues;
      private readonly List<BigInteger> _bSmoothValues;
      private int _totalBSmoothValuesFound;
      private Matrix _matrix;

      /// <summary>
      /// A count of the number of completed iterations of sieving.
      /// </summary>
      private int _sieveIntervals;

      private IEnumerator<Polynomial> _polynomials;
      private int _totalPolynomials;

      public event EventHandler<NotifyProgressEventArgs> Progress;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="n">The number to be factored by this Quadratic Sieve.</param>
      public QuadraticSieve(BigInteger n)
      {
         _nOrig = n;
         _multiplier = 1;
         _n = n;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.

         _factorBase = null;
         _xValues = new();
         _bSmoothValues = new();
         _totalBSmoothValuesFound = 0;
         _matrix = null;

         _sieveIntervals = 0;
         _polynomials = null;
         _totalPolynomials = 0;
      }

      private static Matrix AllocateMatrix(int fbSize)
      {
         // The buffer of 20 is twice the number of extra exponent vectors
         // that will end the sieving loop.
         return new Matrix(fbSize, fbSize, 20);
      }

      protected void OnNotifyProgress(string message)
      {
         Progress?.Invoke(this, new NotifyProgressEventArgs(message));
      }

      public int TotalBSmoothValuesFound { get => _totalBSmoothValuesFound; }

      public int TotalPolynomials { get => _totalPolynomials; }

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

         _M = FindSieveInterval(_n);
         _polynomials = (new MultiPolynomial(_n, _rootN, _factorBase[_factorBase.Count - 1], _M)).GetEnumerator();

         FindBSmooth();

         int retryCount = 0;
         int retryLimit = 100;
         while (retryCount < retryLimit)
         {
            OnNotifyProgress($"Found {_bSmoothValues.Count} relations; reducing Matrix");
            _matrix.Reduce();
            List<BigBitArray> nullVectors = _matrix.FindNullVectors();
            OnNotifyProgress($"Found {nullVectors.Count} Null Vectors");

            BigInteger x, y;
            foreach (BigBitArray nullVector in nullVectors)
            {
               x = BigInteger.One;
               y = BigInteger.One;
               for (int j = 0; j < _bSmoothValues.Count; j++)
               {
                  if (nullVector[j])
                  {
                     x *= _xValues[j];
                     y *= _bSmoothValues[j];
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
                  return (f1, _nOrig / f1);
               }
            }

            retryCount++;
            OnNotifyProgress($"Retry number: {retryCount}");
            PrepareToResieve();
            FindBSmooth();
         }

         // TODO We've gone back and retried 100 times and run out of squares each
         //  time.  That's a minimum of 2**1000 : 1 odds of finding a factor.
         //  This is cause for suspicion.
         throw new ApplicationException($"Ran out of squares while factoring {_n:N0}\nTotal B-Smooth Values Found: {_totalBSmoothValuesFound}\nFactor Base Count: {_factorBase.Count}");
      }

      /// <summary>
      /// Determines an appropriate factor base for factoring the given number
      /// </summary>
      private void FindFactorBase()
      {
         BigInteger n;

         int[] nSmallMultipliersToConsider = new int[] { 1, 3, 5, 7, 11, 13, 17, 19 };
         List<long>[] rv = new List<long>[nSmallMultipliersToConsider.Length];

         long maxPrime = 0;
         for (int j = 0; j < nSmallMultipliersToConsider.Length; j ++)
         {
            n = _nOrig * nSmallMultipliersToConsider[j];
            if ((n & 3) == 1)  // n == 1 mod 4.
            {
               int sz = FindSizeOfFactorBase(n);
               rv[j] = new List<long>(sz);

               // Always add -1
               rv[j].Add(-1);

               // We can always include 2 because any odd number squared will have 1 as a
               // Quadratic Residue modulo 2.
               rv[j].Add(2);

               // Add primes p such that (n % p) is a quadratic residue modulo p.
               // Note that for primes in the second argument, the Jacobi Symbol
               // reduces to the Legendre Symbol.
               IEnumerator<long> primes = Primes.GetEnumerator();
               primes.MoveNext();  // Skip 2
               while (primes.MoveNext() && rv[j].Count < sz)
               {
                  long prime = primes.Current;
                  if (1 == LongCalculator.JacobiSymbol((long)(n % prime), prime))
                  {
                     rv[j].Add(prime);
                     maxPrime = Math.Max(prime, maxPrime);
                  }
               }
            }
         }

         // The goals of this phase are
         //  1. Maximize the number of small primes in the factor base
         //  2. Keep the multiplier small
         //  TODO: Should "Minimize the number of factors in the factor base"
         //        be a goal?
         //  TODO: Is this too much emphasis on keeping the multiplier small?
         double bestCost = double.MaxValue;
         int indexOfBest = int.MinValue;
         for (int j = 0; j < rv.Length; j ++)
         {
            if (rv[j] is not null)
            {
               int smallPrimeCount = 0;
               for (int k = 0; k < rv[j].Count && rv[j][k] < 100; k++)
                  smallPrimeCount++;
               double cost = (double)nSmallMultipliersToConsider[j] / smallPrimeCount;
               if (cost < bestCost)
               {
                  bestCost = cost;
                  indexOfBest = j;
               }
            }
         }

         // TODO There exists a possibility that no small multiplier satisfied
         //  the condition _nOrig * m mod 4 == 1;  In this event, this will
         //  throw an exception.
         _multiplier = nSmallMultipliersToConsider[indexOfBest];
         _n = _multiplier * _nOrig;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.
         _factorBase = rv[indexOfBest];
         _matrix = AllocateMatrix(_factorBase.Count);
      }

      private static int FindSizeOfFactorBase(BigInteger kn)
      {
         // Table 1 of Ref B is unclear as to the meaning of "Factor Base Size".
         // Is this the number of primes in the Factor Base?  Or is it the
         // maximum size of the primes to consider for membership in the
         // Factor Base?
         // Here, the first option is chosen.
         // Additionally, for numbers smaller than 24 digits, the size has
         // been extrapolated based upon halving the size of the factor base
         // for each 6 digit reduction in size.
         int[] digits = new int[] { 12, 18, 24, 30, 36, 42, 48, 54, 60, 66 };
         int[] sz = new int[] { 25, 50, 100, 200, 400, 900, 1200, 2000, 3000, 4500 };

         int numDigits = 1 + (int)Math.Floor(BigInteger.Log10(kn));
         int j = 0;
         while (j < digits.Length && digits[j] < numDigits)
            j++;

         if (j == digits.Length)
            return sz[sz.Length - 1];

         return sz[j];
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

         do
         {
            if (!_polynomials.MoveNext())
            {
               if (_bSmoothValues.Count > _factorBase.Count)
                  return;
               throw new ApplicationException($"Ran out of polynomials while factoring {_n:N0}\nTotal B-Smooth Values Found: {_totalBSmoothValuesFound}\nFactor Base Count: {_factorBase.Count}");
            }
            Polynomial poly = _polynomials.Current;
            _totalPolynomials++;

            // Calculate the values of Q(x)
            List<BigInteger> Q = new(2 * _M + 1);
            for (int x = -_M; x <= _M; x++)
               Q.Add(poly.Evaluate(x));

            //
            // Sieve for B-Smooth values
            //

            // Each bit in a bit array corresponds to one factor in the factor base.
            // The bit indices are the same as the indices into the factorBase.
            // There is one exponent vector for each value of Q.
            List<BigBitArray> exponentVectors = new List<BigBitArray>(2 * _M);
            for (long j = -_M; j <= _M; j++)
               exponentVectors.Add(new BigBitArray(fbSize));

            // Sieve out the special case of p == -1
            for (int j = -_M, idx = 0; j <= _M; j ++, idx ++)
            {
               if (Q[idx] < 0)
               {
                  Q[idx] *= -1;
                  exponentVectors[idx].FlipBit(0);
               }
            }

            // Sieve out the special case of p == 2;
            // the zero'th element of the Factor Base.
            for (int j = -_M, idx = 0; j <= _M; j++, idx ++)
               while ((Q[idx] & 1) == 0 && Q[idx] != 0)
               {
                  Q[idx] >>= 1;
                  exponentVectors[idx].FlipBit(1);
               }

            // Sieve the remaining Factor Base
            for (int factorIndex = 2; factorIndex < fbSize; factorIndex++)
            {
               // Calculate ceiling(a/p) * p - a
               // where a is the start of the Sieve Interval.
               //long rem = -M % _factorBase[factorIndex];
               //if (rem < 0) rem += _factorBase[factorIndex];

               // Find the roots of Q(x) mod p.
               BigInteger inv2a = BigIntegerCalculator.FindInverse(2 * poly.A, _factorBase[factorIndex]);
               long rootnModP = BigIntegerCalculator.SquareRoot(_n, _factorBase[factorIndex]);
               int x1 = (int)((-poly.B + rootnModP) * inv2a % _factorBase[factorIndex]);
               //if (x1 < 0) x1 += (int)_factorBase[factorIndex];
               int x2 = (int)((-poly.B - rootnModP) * inv2a % _factorBase[factorIndex]);
               //if (x2 < 0) x2 += (int)_factorBase[factorIndex];

               // Translate to the first index of Q and exponentVectors where
               // the values will divide evenly
               int offset = (int)(_M % _factorBase[factorIndex]);
               int index1 = x1 + offset;
               if (index1 < 0) index1 += (int)_factorBase[factorIndex];
               if (index1 >= _factorBase[factorIndex]) index1 -= (int)_factorBase[factorIndex];
               int index2 = x2 + offset;
               if (index2 < 0) index2 += (int)_factorBase[factorIndex];
               if (index2 >= _factorBase[factorIndex]) index2 -= (int)_factorBase[factorIndex];

               // Sieve out these factors
               while (index1 < Q.Count)
               {
                  BigInteger q, r;
                  q = BigInteger.DivRem(Q[index1], _factorBase[factorIndex], out r);
                  Assertions.True(r == 0);
                  do
                  {
                     Q[index1] = q;
                     exponentVectors[index1].FlipBit(factorIndex);
                     q = BigInteger.DivRem(Q[index1], _factorBase[factorIndex], out r);
                  } while (r == 0);
                  index1 += (int)_factorBase[factorIndex];
               }

               while (index2 < Q.Count)
               {
                  BigInteger q, r;
                  q = BigInteger.DivRem(Q[index2], _factorBase[factorIndex], out r);
                  Assertions.True(r == 0);
                  do
                  {
                     Q[index2] = q;
                     exponentVectors[index2].FlipBit(factorIndex);
                     q = BigInteger.DivRem(Q[index2], _factorBase[factorIndex], out r);
                  } while (r == 0);
                  index2 += (int)_factorBase[factorIndex];
               }
            }

            // Collect up the B-Smooth numbers and their Exponent Vectors.
            // Each Exponent Vector becomes a column in the output Matrix.
            for (int x = -_M, idx = 0; x < _M; x++, idx ++)
            {
               if (Q[idx] == 1)
               {
                  _totalBSmoothValuesFound++;
                  _xValues.Add(poly.EvaluateLHS(x));
                  _bSmoothValues.Add(poly.Evaluate(x));
                  int index = _bSmoothValues.Count - 1;
                  Assertions.True((_bSmoothValues[index] - _xValues[index] * _xValues[index]) % _n == 0);
                  _matrix.ExpandColumns(index + 1);
                  for (int r = 0; r < fbSize; r++)
                     _matrix[r, index] = exponentVectors[idx][r];
               }
            }

            _sieveIntervals ++;

            // 10 gives a worst-case of 1 chance in 1024 of none of the squares
            // being useful.
         } while (_bSmoothValues.Count < fbSize + 10);
      }

      private static int FindSieveInterval(BigInteger kn)
      {
         // See Table 1 of Ref B
         // Note that these entries are too large to fit into a long.
         // They are included in anticipation of updating to a BigInteger.
         int[] digits = new int[] { 24, 30, 36, 42, 48, 54, 60, 66 };
         int[] interval = new int[] { 5000, 25000, 25000, 50000, 100_000, 250_000, 350_000, 500_000 };

         double numDigits = BigInteger.Log10(kn);
         int j = 0;
         while (j < digits.Length && digits[j] < numDigits)
            j++;

         if (j == digits.Length)
            return interval[interval.Length];

         return interval[j];
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
         _matrix = AllocateMatrix(_factorBase.Count);
         for (int j = freeColumns.Count - 1; j >= 0; j--)
         {
            _bSmoothValues.RemoveAt(j);
            _xValues.RemoveAt(j);
         }

         // Recalculate the Exponent Vectors.
         for (int col = 0, jul = _bSmoothValues.Count; col < jul; col++)
         {
            BigInteger bSmooth = _bSmoothValues[col];

            // Handle factor of -1
            if (bSmooth < 0)
            {
               _matrix.FlipBit(0, col);
               bSmooth *= -1;
            }

            int row = 1, kul = _factorBase.Count;
            BigInteger q, r;
            while (row < kul && bSmooth != 1)
            {
               q = BigInteger.DivRem(bSmooth, _factorBase[row], out r);
               while (r == 0)
               {
                  bSmooth = q;
                  _matrix.FlipBit(row, col);
                  q = BigInteger.DivRem(bSmooth, _factorBase[row], out r);
               }
               row++;
            }
         }
      }
   }
}
