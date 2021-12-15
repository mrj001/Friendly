using System;
namespace Friendly.Library
{
   public static class LongCalculator
   {
      public static long GCD(long a, long b)
      {
#if DEBUG
         if (a < b)
            throw new ArgumentException($"{nameof(a)} must be greater than {nameof(b)}");
#endif
         if (b == 0)
            return a;

         return GCD(b, a % b);
      }

      public static long ModPow(long val, long exponent, long modulus)
      {
         long curBitVal = val;
         long rv = (exponent & 1) == 1 ? val : 1;
         exponent >>= 1;

         while (exponent != 0)
         {
            curBitVal *= curBitVal;
            curBitVal %= modulus;

            if ((exponent & 1) != 0)
            {
               rv *= curBitVal;
               rv %= modulus;
            }

            exponent >>= 1;
         }

         return rv;
      }
      /// <summary>
      /// Determines if the a is a quadratic residue, modulo the prime p.
      /// </summary>
      /// <param name="a">The value to check.</param>
      /// <param name="p">The modulus, which must be an odd prime.</param>
      /// <returns>True if a is a quadratic residue mod p; false otherwise.</returns>
      /// <remarks>
      /// <para>
      /// Reference:
      /// https://exploringnumbertheory.wordpress.com/2013/10/12/eulers-criterion/
      /// </para>
      /// </remarks>
      public static bool IsQuadraticResidue(long a, long p)
      {
#if DEBUG
         if ((p & 1) == 0)
            throw new ArgumentException($"{nameof(p)} must be an odd number");
         if (!Primes.IsPrimeFast(p))   // We must use IsPrimeFast to prevent recursion.
            throw new ArgumentException($"{nameof(p)} must be a prime.");
         if (GCD(p, a) != 1)
            throw new ArgumentException($"{nameof(a)} and {nameof(p)} must be relatively prime.");
#endif

         long exponent = p >> 1;  // (p - 1) / 2
         long e = ModPow(a, exponent, p);

         if (e == 1)
            return true;
         else if (e == p - 1) // e == -1
            return false;
         else
            throw new ApplicationException();
      }
   }
}
