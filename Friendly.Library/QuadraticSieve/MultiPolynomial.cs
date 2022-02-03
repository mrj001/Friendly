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
      private readonly long _kn;
      private readonly long _rootkn;
      private readonly long _maxFactorBase;

      /// <summary>
      /// Constructs a new Enumerable object for the Multiple Polynomials.
      /// </summary>
      /// <param name="kn">The number being factored, pre-multiplied by the small constant.</param>
      /// <param name="rootkn"></param>
      /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
      public MultiPolynomial(long kn, long rootkn, long maxFactorBase)
      {
         _kn = kn;
         _rootkn = rootkn;
         _maxFactorBase = maxFactorBase;
      }

      public IEnumerator<Polynomial> GetEnumerator()
      {
         return new Enumerator(_kn, _rootkn, _maxFactorBase);
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return new Enumerator(_kn, _rootkn, _maxFactorBase);
      }

      private class Enumerator : IEnumerator<Polynomial>
      {
         private int _serial;
         private readonly long _kn;
         private readonly long _rootkn;
         private readonly long _maxFactorBase;
         private IEnumerator<long> _primes;

         /// <summary>
         /// 
         /// </summary>
         /// <param name="kn">The number being factored, premultiplied by a small constant.</param>
         /// <param name="rootkn"></param>
         /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
         public Enumerator(long kn, long rootkn, long maxFactorBase)
         {
            _kn = kn;
            _rootkn = rootkn;
            _serial = -1;
            _maxFactorBase = maxFactorBase;
            _primes = Primes.GetEnumerator();
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
            long d = _primes.Current;
            long a = d * d;

            // Ref. B, Equations 7a & 7b.
            long h0 = LongCalculator.ModPow(_kn, (d - 3) / 4, d);
            if ((h0 & 1) == 1)
               h0 += d;
            //long h1 = ((_kn % d) * h0) % d;
            long h1 = LongCalculator.ModPow(_kn, (d + 1) / 4, d);
            Assertions.True((h0 * h1) % d == 1);

            // Ref B. Equation 8
            long h1squared = h1 * h1;
            Assertions.True(h1squared % d == _kn % d);

            // Ref B. Equation 9
            long num = _kn - h1squared;
            Assertions.True(num % d == 0);
            Assertions.True((h0 & 1) == 0);
            Assertions.True(((h0 / 2) * (2 * h1)) % d == 1);
            long h2 = ((h0 / 2) * (num / d)) % d;

            // Ref B. Equation 10
            long b = (h1 + h2 * d) % a;
            if ((b & 1) == 0) b = a - b;

            // Ref B. Equation 11
            //long bsquared = h1 * h1 + 2 * h1 * h2 * d + h2 * h2 * d * d;
            Assertions.True((h1 * h1 + 2 * h1 * h2 * d + h2 * h2 * d * d) % a == b * b % a);
            long bsquared = b * b;
            Assertions.True(bsquared % a == _kn % a);

            // Ref B. Equation 12
            long c = bsquared - _kn;
            Assertions.True((c % (4 * a)) == 0);
            c /= 4 * a;

            return new Polynomial(a, b, c);
         }

         public void Dispose()
         {
            // Empty
         }

         public bool MoveNext()
         {
            if (_serial < 0)
            {
               _serial = 0;
               _primes.MoveNext();  // Advance to first prime
            }

            // Per Ref. B
            // D will be represented by _primes.Current.
            // Choose D to be:
            //   1. A prime
            //   2. Larger than the Factor Base
            //   3. Legendre(kN/D) == 1 (i.e. kN is a quadratic residue mod D)
            //   4. D == 3 mod 4
            bool havePrimes = _primes.MoveNext();
            while (havePrimes && (_primes.Current <= _maxFactorBase ||
                     1 != LongCalculator.JacobiSymbol(_kn, _primes.Current) ||
                     (_primes.Current & 3) != 3))
               havePrimes = _primes.MoveNext();

            _serial++;
            return havePrimes;
         }

         public void Reset()
         {
            _serial = -1;
            _primes.Reset();
         }
      }
   }
}
