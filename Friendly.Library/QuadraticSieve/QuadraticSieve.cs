﻿using System;
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
   public class QuadraticSieve
   {
      /// <summary>
      /// The original number being factored.
      /// </summary>
      private readonly long _nOrig;

      /// <summary>
      /// A small prime which is multiplied by the number being factored to
      /// obtain a factor base which is richer in small primes.
      /// </summary>
      private long _multiplier;

      /// <summary>
      /// _nOrig *_multiplier
      /// </summary>
      private long _n;

      /// <summary>
      /// Ceiling(sqrt(_n))
      /// </summary>
      private long _rootN;

      private List<long> _factorBase;

      private readonly List<long> _xValues;
      private readonly List<long> _bSmoothValues;
      private Matrix _matrix;

      /// <summary>
      /// A count of the number of completed iterations of sieving.
      /// </summary>
      private int _sieveIntervals;

      private IEnumerator<Polynomial> _polynomials;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="n">The number to be factored by this Quadratic Sieve.</param>
      public QuadraticSieve(long n)
      {
         _nOrig = n;
         _multiplier = 1;
         _n = n;
         _rootN = 1 + LongCalculator.SquareRoot(_n);  // assumes _n is not square.

         _factorBase = null;
         _xValues = new List<long>();
         _bSmoothValues = new List<long>();
         _matrix = null;

         _sieveIntervals = 0;
         _polynomials = null;
      }

      private static Matrix AllocateMatrix(int fbSize)
      {
         // The buffer of 20 is twice the number of extra exponent vectors
         // that will end the sieving loop.
         return new Matrix(fbSize, fbSize, 20);
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
      public (long, long) Factor()
      {
         FindFactorBase();

         _polynomials = (new MultiPolynomial(_n, _rootN, _factorBase[_factorBase.Count - 1])).GetEnumerator();

         FindBSmooth();

         int retryCount = 0;
         int retryLimit = 100;
         while (retryCount < retryLimit)
         {
            _matrix.Reduce();
            List<BigBitArray> nullVectors = _matrix.FindNullVectors();

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

               long xmy = (long)(x - y);
               if (xmy < 0)
                  xmy += _n;

               long f1 = LongCalculator.GCD(_n, xmy);
               if (f1 != 1 && f1 != _n && f1 != _nOrig && f1 != _multiplier)
               {
                  long q = Math.DivRem(f1, _multiplier, out long remainder);
                  if (remainder == 0)
                     f1 = q;
                  return (f1, _nOrig / f1);
               }
            }

            retryCount++;
            PrepareToResieve();
            FindBSmooth();
         }

         // TODO We've gone back and retried 10 times and run out of squares each
         //  time.  That's a minimum of 2**100 : 1 odds of finding a factor.
         //  This is cause for suspicion.
         throw new ApplicationException($"Ran out of squares while factoring {_n:N0}");
      }

      /// <summary>
      /// Determines an appropriate factor base for factoring the given number
      /// </summary>
      private void FindFactorBase()
      {
         long n;

         int[] nSmallMultipliersToConsider = new int[] { 1, 3, 5, 7, 11, 13, 17, 19 };
         List<long>[] rv = new List<long>[nSmallMultipliersToConsider.Length];

         long maxPrime = 0;
         for (int j = 0; j < nSmallMultipliersToConsider.Length; j ++)
         {
            try
            {
                n = _nOrig * nSmallMultipliersToConsider[j];
            }
            catch (OverflowException)
            {
               // Try to factor with just the factor bases we've already got.
               break;
            }
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
                  if (1 == LongCalculator.JacobiSymbol(n % prime, prime))
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
         _rootN = 1 + LongCalculator.SquareRoot(_n);  // assumes _n is not square.
         _factorBase = rv[indexOfBest];
         _matrix = AllocateMatrix(_factorBase.Count);
      }

      private static int FindSizeOfFactorBase(long kn)
      {
         // Table 1 of Ref B is unclear as to the meaning of "Factor Base Size".
         // Is this the number of primes in the Factor Base?  Or is it the
         // maximum size of the primes to consider for membership in the
         // Factor Base?
         // Here, the first option is chosen.
         // Additionally, for numbers smaller than 24 digits, the size has
         // been extrapolated based upon halving the size of the factor base
         // for each 6 digit reduction in size.
         // Note that almost all of the entries here are far too large to fit
         // in a long.  They are included in anticipation of a future update to
         // using BigIntegers.
         int[] digits = new int[] { 12, 18, 24, 30, 36, 42, 48, 54, 60, 66 };
         int[] sz = new int[] { 25, 50, 100, 200, 400, 900, 1200, 2000, 3000, 4500 };

         double d = Math.Log(kn, 10);
         int j = 0;
         while (j < digits.Length && digits[j] < d)
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
         int M = FindSieveInterval(_n);

         do
         {
            if (!_polynomials.MoveNext())
               throw new ApplicationException("Ran out of polynomials");
            Polynomial poly = _polynomials.Current;

            // Calculate the values of Q(x)
            List<long> Q = new List<long>(2 * M);
            for (int x = -M; x < M; x++)
               Q.Add(poly.Evaluate(x));

            //
            // Sieve for B-Smooth values
            //

            // Each bit in a bit array corresponds to one factor in the factor base.
            // The bit indices are the same as the indices into the factorBase.
            // There is one exponent vector for each value of Q.
            List<BigBitArray> exponentVectors = new List<BigBitArray>(2 * M);
            for (long j = -M; j < M; j++)
               exponentVectors.Add(new BigBitArray(fbSize));

            // Sieve out the special case of p == -1
            for (int j = -M, idx = 0; j < M; j ++, idx ++)
            {
               if (Q[idx] < 0)
               {
                  Q[idx] *= -1;
                  exponentVectors[idx].FlipBit(0);
               }
            }

            // Sieve out the special case of p == 2;
            // the zero'th element of the Factor Base.
            for (int j = -M, idx = 0; j < M; j++, idx ++)
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
               long inv2a = LongCalculator.FindInverse(2 * poly.A, _factorBase[factorIndex]);
               long rootnModP = LongCalculator.SquareRoot(_n, _factorBase[factorIndex]);
               int x1 = (int)((-poly.B + rootnModP) * inv2a % _factorBase[factorIndex]);
               //if (x1 < 0) x1 += (int)_factorBase[factorIndex];
               int x2 = (int)((-poly.B - rootnModP) * inv2a % _factorBase[factorIndex]);
               //if (x2 < 0) x2 += (int)_factorBase[factorIndex];

               // Translate to the first index of Q and exponentVectors where
               // the values will divide evenly
               int offset = (int)(M % _factorBase[factorIndex]);
               int index1 = x1 + offset;
               if (index1 < 0) index1 += (int)_factorBase[factorIndex];
               if (index1 >= _factorBase[factorIndex]) index1 -= (int)_factorBase[factorIndex];
               int index2 = x2 + offset;
               if (index2 < 0) index2 += (int)_factorBase[factorIndex];
               if (index2 >= _factorBase[factorIndex]) index2 -= (int)_factorBase[factorIndex];

               // Sieve out these factors
               while (index1 < Q.Count)
               {
                  long q, r;
                  q = Math.DivRem(Q[index1], _factorBase[factorIndex], out r);
                  Assertions.True(r == 0);
                  do
                  {
                     Q[index1] = q;
                     exponentVectors[index1].FlipBit(factorIndex);
                     q = Math.DivRem(Q[index1], _factorBase[factorIndex], out r);
                  } while (r == 0);
                  index1 += (int)_factorBase[factorIndex];
               }

               while (index2 < Q.Count)
               {
                  long q, r;
                  q = Math.DivRem(Q[index2], _factorBase[factorIndex], out r);
                  Assertions.True(r == 0);
                  do
                  {
                     Q[index2] = q;
                     exponentVectors[index2].FlipBit(factorIndex);
                     q = Math.DivRem(Q[index2], _factorBase[factorIndex], out r);
                  } while (r == 0);
                  index2 += (int)_factorBase[factorIndex];
               }
            }

            // Collect up the B-Smooth numbers and their Exponent Vectors.
            // Each Exponent Vector becomes a column in the output Matrix.
            for (int x = -M, idx = 0; x < M; x++, idx ++)
            {
               if (Q[idx] == 1)
               {
                  _xValues.Add(poly.EvaluateLHS(x));
                  _bSmoothValues.Add(poly.Evaluate(x));
                  int index = _bSmoothValues.Count - 1;
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

      private static int FindSieveInterval(long kn)
      {
         // See Table 1 of Ref B
         // Note that these entries are too large to fit into a long.
         // They are included in anticipation of updating to a BigInteger.
         int[] digits = new int[] { 24, 30, 36, 42, 48, 54, 60, 66 };
         int[] interval = new int[] { 5000, 25000, 25000, 50000, 100_000, 250_000, 350_000, 500_000 };

         double numDigits = Math.Log(kn, 10);
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
            long bSmooth = _bSmoothValues[col];

            // Handle factor of -1
            if (bSmooth < 0)
            {
               _matrix.FlipBit(0, col);
               bSmooth *= -1;
            }

            int row = 1, kul = _factorBase.Count;
            long q, r;
            while (row < kul && bSmooth != 1)
            {
               q = Math.DivRem(bSmooth, _factorBase[row], out r);
               while (r == 0)
               {
                  bSmooth = q;
                  _matrix.FlipBit(row, col);
                  q = Math.DivRem(bSmooth, _factorBase[row], out r);
               }
               row++;
            }
         }
      }
   }
}
