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
      /// Raises val to the given exponent.
      /// </summary>
      /// <param name="val">The base of the exponent.</param>
      /// <param name="exponent">The exponent.</param>
      /// <returns>Val to the given exponent</returns>
      /// <remarks>
      /// <para>
      /// A double only has a 53-bit mantissa.  Thus, for values equal to or
      /// greater than 2**54, exact values cannot be expected.
      /// </para>
      /// </remarks>
      public static long Pow(long val, int exponent)
      {
         long curBitVal = val;
         long rv = (exponent & 1) == 1 ? val : 1;
         exponent >>= 1;

         while (exponent != 0)
         {
            curBitVal *= curBitVal;

            if ((exponent & 1) != 0)
               rv *= curBitVal;

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

      #region Jacobi Symbol
      // Calculation of the Jacobi Symbol is explained here (accessed 2021-12-14):
      //   https://exploringnumbertheory.wordpress.com/2015/11/29/the-jacobi-symbol/
      //
      // Calculation of the Legendre Symbol is explained here:
      //   https://exploringnumbertheory.wordpress.com/2015/11/28/the-legendre-symbol/
      public static long JacobiSymbol(long a, long n)
      {
         // Per Theorem 1 of the Reference.
         // If a and n are not mutually prime, the value of the Jacobi Symbol is 0.
         if (GCD((a < n ? n : a), (a >= n ? n : a)) > 1)
            return 0;

         // Reduce per Theorem 1, point 3.
         if (a > n) a %= n;

         if (a == 0) return 0;

         long sign = 1;
         while (n >= Primes.SieveLimit || !Primes.IsPrimeFast(n))
         {
            // Can we factor out powers of two?
            int powersOfTwo = 0;
            while ((n & 1) == 0)
            {
               n >>= 1;
               powersOfTwo++;
            }
            long powerOfTwoSign = (powersOfTwo & 1) == 0 ? 1 : Jacobi2(n);

            // Per the "Law of Quadratic Reciprocity"
            // Point 6 of Theorem 3 of the Jacobi Symbol reference.
            long flipSign = ((a & 3) == 1 || (n & 3) == 1) ? 1 : -1;

            long t = a;
            a = n;
            n = t;
            sign *= powerOfTwoSign * flipSign;
            a %= n;
         }

         return sign * (IsQuadraticResidue(a, n) ? 1 : -1);
      }

      /// <summary>
      /// Calculates the Jacobi symbol when the upper number is a 2.
      /// </summary>
      /// <param name="n">The lower number of the Jacobi Symbol.</param>
      /// <returns></returns>
      /// <remarks>
      /// <para>
      /// This is based upon point 8 (Second supplement to the law of
      /// Quadratic Reciprocity) of the Reference.
      /// </para>
      /// </remarks>
      private static long Jacobi2(long n)
      {
         long remainder = n & 7;   // n mod 8
         switch (remainder)
         {
            case 1:
            case 7:
               return 1;

            case 3:
            case 5:
               return -1;

            default:
               // There should be no way to reach this line.
               throw new ApplicationException($"{nameof(n)} (= {n}) has an unexpected value mod 8.");
         }
      }
      #endregion

      /// <summary>
      /// Calculates an approximate square root of a long
      /// </summary>
      /// <returns>A value, say x, such that x * x <= n < (x+1)*(x+1).</returns>
      /// <remarks>A double has a 53 bit mantissa.  Thus, it may not calculate
      /// square roots with sufficient accuracy for any integer value greater
      /// than 2**53.</remarks>
      public static long SquareRoot(long n)
      {
         // Estimate rough power of ten of the root.
         int pow = 0;
         long n1 = n;
         while (n1 > 100)
         {
            pow++;
            n1 /= 100;
         }

         // Rough initial estimate of square root.
         int k = 1;
         while (k < 10 && k * k < n1)
            k++;
         long root = (k > 1 ? k - 1 : 1) * (long)Math.Pow(10, pow);

         // Iterate to the desired value
         while (root * root > n || (root + 1) * (root + 1) <= n)
            root = (root + n / root) / 2;

         return root;
      }
   }
}
