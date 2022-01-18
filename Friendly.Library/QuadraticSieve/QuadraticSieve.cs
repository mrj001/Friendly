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

namespace Friendly.Library.QuadraticSieve
{
   public static class QuadraticSieve
   {
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
      public static (long, long) Factor(long n)
      {
         List<long> factorBase = FactorBase(n);
         //(List<long> xValues, List<long> bSmooth, Matrix A) = FindBSmooth(factorBase, n);
         SieveToken sieveToken = FindBSmooth(factorBase, n);

         int retryCount = 0;
         int retryLimit = 10;
         while (retryCount < retryLimit)
         {
            sieveToken.ExponentVectorMatrix.Reduce();
            List<BigBitArray> nullVectors = sieveToken.ExponentVectorMatrix.FindNullVectors();

            BigInteger x, y;
            foreach (BigBitArray nullVector in nullVectors)
            {
               x = BigInteger.One;
               y = BigInteger.One;
               for (int j = 0; j < sieveToken.SmoothCount; j++)
               {
                  if (nullVector[j])
                  {
                     x *= sieveToken.GetXValue(j);
                     y *= sieveToken.GetSmoothValue(j);
                  }
               }
               BigInteger t = BigIntegerCalculator.SquareRoot(y);
               Assertions.True(t * t == y);  // y was constructed to be a square.
               y = t;

               x %= n;
               y %= n;

               // Is x = +/-y mod n?
               if (x == y || x + y == n)
                  continue;

               long xmy = (long)(x - y);
               if (xmy < 0)
                  xmy += n;

               long f1 = LongCalculator.GCD(n, xmy);
               if (f1 != 1 && f1 != n)
                  return (f1, n / f1);
            }

            retryCount++;
            sieveToken.PrepareToResieve();
            FindBSmooth(factorBase, n, sieveToken);
         }

         // TODO We've gone back and retried 10 times and run out of squares each
         //  time.  That's a minimum of 2**100 : 1 odds of finding a factor.
         //  This is cause for suspicion.
         throw new ApplicationException($"Ran out of squares while factoring {n:N0}");
      }

      /// <summary>
      /// Determines an appropriate factor base for factoring the given number
      /// </summary>
      /// <param name="n">The number being factored.</param>
      /// <returns>A factor base to use for factoring the given number.</returns>
      internal static List<long> FactorBase(long n)
      {
         double logn = Math.Log(n);
         // Number of odd primes to consider for the Factor Base per Ref. A.
         int sz = (int)Math.Floor(Math.Sqrt(Math.Exp(Math.Sqrt(logn * Math.Log(logn)))));
         List<long> rv = new List<long>(sz);

         // We can always include 2 because any odd number squared will have 1 as a
         // Quadratic Residue modulo 2.
         rv.Add(2);

         // Add primes p such that (n % p) is a quadratic residue modulo p.
         // Note that for primes in the second argument, the Jacobi Symbol
         // reduces to the Legendre Symbol.
         IEnumerator<long> j = Primes.GetEnumerator();
         j.MoveNext();  // Skip 2
         int k = 0;
         while (j.MoveNext() && k < sz)
         {
            if (1 == LongCalculator.JacobiSymbol(n % j.Current, j.Current))
               rv.Add(j.Current);
            k++;
         }

         return rv;
      }

      /// <summary>
      /// Sieves for a list of B-Smooth numbers
      /// </summary>
      /// <param name="factorBase">The factor base which defines B-Smooth.</param>
      /// <param name="n">The number being factored.</param>
      /// <returns>A Tuple containing:
      /// <list type="number">
      /// <item>the x-values (input to the polynomials)</item>
      /// <item>the B-Smooth output values of the polynomials</item>
      /// <item>the exponent vector Matrix, which must be solved.</item>
      /// </list>
      /// <remarks>
      /// <para>
      /// Each column of the Matrix contains the Exponent Vector for the B-Smooth number at
      /// the same index.
      /// </para>
      /// </remarks>
      internal static SieveToken FindBSmooth(List<long> factorBase, long n)
      {
         SieveToken sieveToken = new SieveToken(factorBase);
         FindBSmooth(factorBase, n, sieveToken);
         return sieveToken;
      }

      private static void FindBSmooth(List<long> factorBase, long n, SieveToken sieveToken)
      {
         // Choose twice the largest factor as the Sieving Interval
         int fbSize = factorBase.Count;
         int M = 2 * (int)factorBase[fbSize - 1];
         //List<long> xValues = new List<long>();
         //List<long> lstBSmooth = new List<long>();
         Matrix expVectors = sieveToken.ExponentVectorMatrix;

         // Note: this assumes that n is not a square.
         long rootN = 1 + LongCalculator.SquareRoot(n);

         do
         {
            // Calculate the values of Q(x) = (x + rootN)**2 - n;
            List<long> Q = new List<long>(M);
            for (int x = 0; x < M; x++)
            {
               long t = x + rootN + sieveToken.SieveIntervals * M;
               Q.Add(t * t - n);
            }

            //
            // Sieve for B-Smooth values
            //

            // Each bit in a bit array corresponds to one factor in the factor base.
            // The bit indices are the same as the indices into the factorBase.
            // There is one exponent vector for each value of Q.
            List<BigBitArray> exponentVectors = new List<BigBitArray>(M);
            for (int j = 0; j < M; j++)
               exponentVectors.Add(new BigBitArray(fbSize));

            // Sieve out the special case of p == 2;
            // the zero'th element of the Factor Base.
            for (int j = 0; j < M; j++)
               while ((Q[j] & 1) == 0)
               {
                  Q[j] >>= 1;
                  exponentVectors[j].FlipBit(0);
               }

            // Sieve the remaining Factor Base
            for (int factorIndex = 1; factorIndex < fbSize; factorIndex++)
            {
               // Calculate ceiling(a/p) * p - a
               // where a is the start of the Sieve Interval.
               long rem = (rootN + sieveToken.SieveIntervals * M) % factorBase[factorIndex];
               if (rem != 0) rem = factorBase[factorIndex] - rem;

               // Find the square roots of n mod p.
               int x1 = (int)LongCalculator.SquareRoot(n % factorBase[factorIndex], factorBase[factorIndex]);
               int x2 = (int)factorBase[factorIndex] - x1;

               x1 += (int)rem;
               if (x1 >= factorBase[factorIndex]) x1 -= (int)factorBase[factorIndex];
               x2 += (int)rem;
               if (x2 >= factorBase[factorIndex]) x2 -= (int)factorBase[factorIndex];

               // Sieve out these factors
               while (x1 < Q.Count)
               {
                  long q, r;
                  q = Math.DivRem(Q[x1], factorBase[factorIndex], out r);
                  Assertions.True(r == 0);
                  do
                  {
                     Q[x1] = q;
                     exponentVectors[x1].FlipBit(factorIndex);
                     q = Math.DivRem(Q[x1], factorBase[factorIndex], out r);
                  } while (r == 0);
                  x1 += (int)factorBase[factorIndex];
               }

               while (x2 < Q.Count)
               {
                  long q, r;
                  q = Math.DivRem(Q[x2], factorBase[factorIndex], out r);
                  Assertions.True(r == 0);
                  do
                  {
                     Q[x2] = q;
                     exponentVectors[x2].FlipBit(factorIndex);
                     q = Math.DivRem(Q[x2], factorBase[factorIndex], out r);
                  } while (r == 0);
                  x2 += (int)factorBase[factorIndex];
               }
            }

            // Collect up the B-Smooth numbers and their Exponent Vectors.
            // Each Exponent Vector becomes a column in the output Matrix.
            for (int x = 0; x < M; x++)
            {
               if (Q[x] == 1)
               {
                  long t = x + rootN + sieveToken.SieveIntervals * M;
                  sieveToken.AddValues(t, t * t - n);
                  int index = sieveToken.SmoothCount - 1;
                  expVectors.ExpandColumns(index + 1);
                  for (int r = 0; r < fbSize; r++)
                     expVectors[r, index] = exponentVectors[x][r];
               }
            }

            sieveToken.IncrementSieveIntervals();

            // 10 gives a worst-case of 1 chance in 1024 of none of the squares
            // being useful.
         } while (sieveToken.SmoothCount < fbSize + 10);
      }

      // NOTE: SieveToken is only internal instead of private to support
      //   unit test of FindBSmooth.

      /// <summary>
      /// A class to track the state of the Sieving in order to handle
      /// additional sieving if the previous set(s) of smooth numbers turn out
      /// to be inadequate to factor the number.
      /// </summary>
      internal class SieveToken
      {
         private readonly List<long> _factorBase;
         private readonly List<long> _xValues;
         private readonly List<long> _bSmoothValues;
         private Matrix _matrix;
         private int _sieveIntervals;

         public SieveToken(List<long> factorBase)
         {
            _factorBase = factorBase;
            _xValues = new List<long>(_factorBase.Count + 10);
            _bSmoothValues = new List<long>(_factorBase.Count + 10);
            _matrix = AllocateMatrix(_factorBase.Count);
            _sieveIntervals = 0;
         }

         private static Matrix AllocateMatrix(int fbSize)
         {
            return new Matrix(fbSize, fbSize, 20);
         }

         /// <summary>
         /// Gets the number of Sieve Intervals that have already been sieved.
         /// </summary>
         public int SieveIntervals { get => _sieveIntervals; }

         /// <summary>
         /// Adds one to the completed number of Sieve Intervals
         /// </summary>
         public void IncrementSieveIntervals()
         {
            _sieveIntervals++;
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
            // Remove the free columns which generated the non-useful null vectors.
            List<int> freeColumns = _matrix.FindFreeColumns();
            _matrix = AllocateMatrix(_factorBase.Count);
            for (int j = freeColumns.Count - 1; j >= 0; j--)
            {
               _bSmoothValues.RemoveAt(j);
               _xValues.RemoveAt(j);
            }

            // Recalculate the Exponent Vectors.
            for (int col = 0, jul = _bSmoothValues.Count; col < jul; col ++)
            {
               long bSmooth = _bSmoothValues[col];
               int row = 0, kul = _factorBase.Count;
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

         public Matrix ExponentVectorMatrix { get => _matrix; }

         /// <summary>
         /// Gets the count of B-Smooth values.
         /// </summary>
         public int SmoothCount { get => _bSmoothValues.Count; }

         public void AddValues(long xValue, long bSmoothNumber)
         {
            _xValues.Add(xValue);
            _bSmoothValues.Add(bSmoothNumber);
         }

         public long GetXValue(int index)
         {
            return _xValues[index];
         }

         public long GetSmoothValue(int index)
         {
            return _bSmoothValues[index];
         }
      }
   }
}
