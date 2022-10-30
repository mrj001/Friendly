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

      private FactorBase _factorBase;

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
         _polynomials = (new MultiPolynomial(_n, _rootN, _factorBase.MaxPrime, _M)).GetEnumerator();

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
         _factorBase = FactorBaseCandidate.GetFactorBase(_nOrig);
         _multiplier = _factorBase.Multiplier;
         _n = _multiplier * _nOrig;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.
         _matrix = AllocateMatrix(_factorBase.Count);
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

         double T = FindLargePrimeTolerance(_n);
         long pmax = _factorBase[_factorBase.Count - 1].Prime;
         double pmaxt = Math.Pow(pmax, T);

         int numRelationsNeeded = (int)Math.Round(0.96 * fbSize);

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

            // Initialize the Sieve array to zero over the range [-M, M]
            for (int j = 0; j < sieveSize; j++)
               sieve[j] = 0;

            // The Sieve Threshold is per Ref. B, Section 4 (iii)
            ushort sieveThreshold = (ushort)Math.Round(Math.Log(_M * Math.Sqrt((double)_n / 2) / pmaxt));

            // for all primes in the factor base (other than -1 and 2) add Add log(p) to the sieve
            for (int j = 2, jul = _factorBase.Count; j < jul; j ++)
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
               while (index1 < sieveSize)
               {
                  sieve[index1] += log;
                  index1 += curPrime;
               }
               while (index2 < sieveSize)
               {
                  sieve[index2] += log;
                  index2 += curPrime;
               }
            }

            // Find all sieve locations which exceed the Sieve Threshold
            for (int x = -_M, idx = 0; x <= _M; x ++, idx++)
            {
               if (sieve[idx] > sieveThreshold)
               {
                  BigBitArray exponentVector = new BigBitArray(_factorBase.Count);
                  BigInteger Q = poly.Evaluate(x);

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
                  {
                     _totalBSmoothValuesFound++;
                     _xValues.Add(poly.EvaluateLHS(x));
                     _bSmoothValues.Add(poly.Evaluate(x));
                     int index = _bSmoothValues.Count - 1;
                     Assertions.True((_bSmoothValues[index] - _xValues[index] * _xValues[index]) % _n == 0);
                     _matrix.ExpandColumns(index + 1);
                     for (int r = 0; r < fbSize; r++)
                        _matrix[r, index] = exponentVector[r];
                  }
               }
            }

            _sieveIntervals ++;

         } while (_bSmoothValues.Count < numRelationsNeeded);
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
            return interval[interval.Length - 1];

         return interval[j];
      }

      /// <summary>
      /// Finds the value of T, the large prime tolerance in Ref. B
      /// </summary>
      /// <param name="kn"></param>
      /// <returns></returns>
      private static double FindLargePrimeTolerance(BigInteger kn)
      {
         int[] digits = new int[] { 24, 30, 36, 42, 48, 54, 60, 66 };
         double[] T = new double[] { 1.5, 1.5, 1.75, 2.0, 2.0, 2.2, 2.4, 2.6 };

         int numDigits = BigIntegerCalculator.GetNumberOfDigits(kn);
         int j = 0;
         while (j < digits.Length && digits[j] < numDigits)
            j++;

         if (j == digits.Length)
            return T[T.Length - 1];

         return T[j];
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
               q = BigInteger.DivRem(bSmooth, _factorBase[row].Prime, out r);
               while (r == 0)
               {
                  bSmooth = q;
                  _matrix.FlipBit(row, col);
                  q = BigInteger.DivRem(bSmooth, _factorBase[row].Prime, out r);
               }
               row++;
            }
         }
      }
   }
}
