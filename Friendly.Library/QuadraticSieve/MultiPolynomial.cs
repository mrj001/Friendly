using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

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
   public class MultiPolynomial : IEnumerable<Polynomial>
   {
      private readonly BigInteger _kn;
      private readonly BigInteger _rootkn;
      private readonly long _maxFactorBase;
      private readonly int _M;

      /// <summary>
      /// Constructs a new Enumerable object for the Multiple Polynomials.
      /// </summary>
      /// <param name="kn">The number being factored, pre-multiplied by the small constant.</param>
      /// <param name="rootkn">The ceiling of the square root of kn.</param>
      /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
      /// <param name="M">The Sieve Interval</param>
      public MultiPolynomial(BigInteger kn, BigInteger rootkn, long maxFactorBase, int M)
      {
         _kn = kn;
         _rootkn = rootkn;
         _maxFactorBase = maxFactorBase;
         _M = M;
      }

      public IEnumerator<Polynomial> GetEnumerator()
      {
         return new Enumerator(_kn, _rootkn, _maxFactorBase, _M);
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return new Enumerator(_kn, _rootkn, _maxFactorBase, _M);
      }

      private class Enumerator : IEnumerator<Polynomial>
      {
         private int _serial;
         private readonly BigInteger _kn;
         private readonly BigInteger _rootkn;
         private readonly long _maxFactorBase;
         private readonly int _M;

         /// <summary>
         /// The "ideal" choice of D in Ref. B, section 3.
         /// </summary>
         private readonly long _idealD;
         private long _currentD = 0;
         private long _lowerD = long.MaxValue;
         private long _higherD = long.MinValue;
         private bool _nextDHigher = false;

         /// <summary>
         /// 
         /// </summary>
         /// <param name="kn">The number being factored, premultiplied by a small constant.</param>
         /// <param name="rootkn">The ceiling of the square root of kn.</param>
         /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
         /// <param name="M">The Sieve Interval</param>
         public Enumerator(BigInteger kn, BigInteger rootkn, long maxFactorBase, int M)
         {
            _kn = kn;
            _rootkn = rootkn;
            _serial = -1;
            _maxFactorBase = maxFactorBase;
            _M = M;

            BigInteger t = _rootkn / (4 * M);
            _idealD = (long)BigIntegerCalculator.SquareRoot(t);
            if ((_idealD & 1) == 0)
               _idealD--;
            Reset();
         }

         public Polynomial Current => InternalCurrent();

         object IEnumerator.Current => InternalCurrent();

         private Polynomial InternalCurrent()
         {
            //// First option in Ref A
            //long d = _primes.Current;
            //long a = d * d;

            //// Solve for B in B**2 == kn mod a;
            //long b = LongCalculator.SquareRoot(_kn, d);
            //Assertions.True(b * b % d == _kn % d);

            //// Apply Hensel's Lemma to lift the root to mod a
            //long t = _kn - b * b;
            //Assertions.True(t % d == 0);
            //t /= d;
            //long inv2b = LongCalculator.FindInverse(2 * b, d);
            //t *= inv2b;
            //t %= d;
            //b += t * d;
            //if ((b & 1) == 0) b = a - b;    // TODO: why?
            //// Confirm the root was lifted correctly.
            //Assertions.True(b * b % a == _kn % a);

            //long c = (b * b - _kn);
            //Assertions.True(c % a == 0);
            //c /= a;



            //// Second option in Ref A
            //// Choose a to be a prime
            //long a = _primes.Current;

            //// Solve for b in b**2 == n mod a
            //long b = LongCalculator.SquareRoot(_kn, a);

            //Assertions.True((b * b - _kn) % a == 0);
            //long c = (b * b - _kn) / a;


            // Finding Coefficients per Ref B.
            long d = _currentD;
            BigInteger a = d * d;

            // Ref. B, Equations 7a & 7b.
            BigInteger h0 = BigInteger.ModPow(_kn, (d - 3) / 4, d);
            if ((h0 & 1) == 1)
               h0 += d;
            //long h1 = ((_kn % d) * h0) % d;
            BigInteger h1 = BigInteger.ModPow(_kn, (d + 1) / 4, d);
            Assertions.True((h0 * h1) % d == 1);

            // Ref B. Equation 8
            BigInteger h1squared = h1 * h1;
            Assertions.True(h1squared % d == _kn % d);

            // Ref B. Equation 9
            BigInteger num = _kn - h1squared;
            Assertions.True(num % d == 0);
            Assertions.True((h0 & 1) == 0);
            Assertions.True(((h0 / 2) * (2 * h1)) % d == 1);
            BigInteger h2 = (long)(((h0 / 2) * (num / d)) % d);

            // Ref B. Equation 10
            BigInteger b = (h1 + h2 * d) % a;
            if ((b & 1) == 0) b = a - b;

            // Ref B. Equation 11
            //long bsquared = h1 * h1 + 2 * h1 * h2 * d + h2 * h2 * d * d;
            Assertions.True((h1 * h1 + 2 * h1 * h2 * d + h2 * h2 * d * d) % a == b * b % a);
            BigInteger bsquared = b * b;
            Assertions.True(bsquared % a == _kn % a);

            // Ref B. Equation 12
            BigInteger c = bsquared - _kn;
            Assertions.True((c % (4 * a)) == 0);
            c /= 4 * a;

            return new Polynomial(a, b, c, BigIntegerCalculator.FindInverse(2 * d, _kn));
         }

         public void Dispose()
         {
            // Empty
         }

         public bool MoveNext()
         {
            long d;

            _serial++;

            if (!_nextDHigher)
            {
               d = _lowerD;
               do
                  d -= 2;
               while (d > _maxFactorBase && !IsSuitablePrime(d));
               _lowerD = d;
               _nextDHigher = true;
               if (d > _maxFactorBase)
               {
                  _currentD = _lowerD;
                  return true;
               }
            }

            d = _higherD;
            do
               d += 2;
            while (!IsSuitablePrime(d));
            _higherD = d;
            _currentD = d;
            _nextDHigher = _lowerD > _maxFactorBase;

            return true;
         }

         private bool IsSuitablePrime(long p)
         {
            // A suitable prime must meet all of the following conditions:
            //   1. Be Prime.
            //   2. Be larger than the Factor Base
            //   3. Legendre(kN/D) == 1 (i.e. kN is a quadratic residue mod D)
            //   4. D == 3 mod 4
            return Primes.IsPrime(p) && p > _maxFactorBase &&
               1 == BigIntegerCalculator.JacobiSymbol(_kn, p) && (p & 3) == 3;
         }

         public void Reset()
         {
            _serial = 0;
            _currentD = long.MinValue;
            _lowerD = _idealD + 2;
            _higherD = Math.Max(_idealD, _maxFactorBase);
            _nextDHigher = _lowerD <= _maxFactorBase;
         }
      }
   }
}
