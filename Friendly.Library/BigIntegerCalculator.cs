using System;
using System.Numerics;

namespace Friendly.Library
{
   public static class BigIntegerCalculator
   {
      /// <summary>
      /// Calculates the Greatest Common Divisor of a and b
      /// </summary>
      /// <param name="a"></param>
      /// <param name="b"></param>
      /// <returns>The Greatest Common Divisor of a and b</returns>
      public static BigInteger GCD(BigInteger a, BigInteger b)
      {
         Assertions.True(a >= 0);
         Assertions.True(b >= 0);
         if (a < b)
            return GCDInternal(a, b);
         else
            return GCDInternal(b, a);
      }

      private static BigInteger GCDInternal(BigInteger a, BigInteger b)
      {
         while (b != BigInteger.Zero)
         {
            BigInteger t = b;
            b = a % b;
            a = t;
         }

         return a;
      }

      /// <summary>
      /// Generates a random prime number with the given number of digits.
      /// </summary>
      /// <param name="numDigits">The number of digits</param>
      /// <returns></returns>
      public static BigInteger RandomPrime(int numDigits)
      {
         BigInteger rv = BigInteger.Zero;

         // Add digits in groups of three
         int j = numDigits;
         while (j > 3)
         {
            rv *= 1000;
            rv += Random.Shared.NextInt64(0, 1000);
            j -= 3;
         }

         // Add remaining single digits
         while (j > 0)
         {
            rv *= 10;
            rv += Random.Shared.NextInt64(0, 10);
            j -= 1;
         }

         // Handle possible leading zeroes
         while (1 + (int)Math.Floor(BigInteger.Log10(rv)) < numDigits)
         {
            rv *= 10;
            rv += Random.Shared.NextInt64(0, 10);
         }

         // Make sure it's odd
         if (rv.IsEven)
            rv += 1;

         // Make sure it's prime
         while (!Primes.IsPrime(rv))
            rv -= 2;

         return rv;
      }

      // See: https://en.wikipedia.org/wiki/Extended_Euclidean_algorithm
      public static BigInteger FindInverse(BigInteger a, BigInteger n)
      {
         BigInteger t = 0;
         BigInteger newt = 1;
         BigInteger r = n;
         BigInteger newr = a;

         while (newr != 0)
         {
            BigInteger quotient = r / newr;

            BigInteger tmp = t;
            t = newt;
            newt = tmp - quotient * newt;

            tmp = r;
            r = newr;
            newr = tmp - quotient * newr;
         }
         if (r > 1)
            throw new ApplicationException($"{a} is not invertible mod {n}");
         if (t < 0) t += n;

         return t;
      }

      private static long ModPow(BigInteger val, long exponent, long modulus)
      {
         val %= modulus;
         long curBitVal = (long)val;
         long rv = (exponent & 1) == 1 ? (long)val : 1;
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
      public static BigInteger Pow(BigInteger val, int exponent)
      {
         BigInteger curBitVal = val;
         BigInteger rv = (exponent & 1) == 1 ? val : 1;
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
      public static bool IsQuadraticResidue(BigInteger a, long p)
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
         BigInteger e = ModPow(a, exponent, p);

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
      public static long JacobiSymbol(BigInteger a, long n)
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
            while ((a & 1) == 0 && a != 0)
            {
               a >>= 1;
               powersOfTwo++;
            }
            if (a == 0) return 0;
            long powerOfTwoSign = (powersOfTwo & 1) == 0 ? 1 : Jacobi2(n);

            // Per the "Law of Quadratic Reciprocity"
            // Point 6 of Theorem 3 of the Jacobi Symbol reference.
            long flipSign = ((a & 3) == 1 || (n & 3) == 1) ? 1 : -1;

            long t = (long)a;
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
      private static long Jacobi2(BigInteger n)
      {
         long remainder = (long)(n & 7);   // n mod 8
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
      public static BigInteger SquareRoot(BigInteger n)
      {
         // Estimate rough power of ten of the root.
         int pow = 0;
         BigInteger n1 = n;
         while (n1 > 100)
         {
            pow++;
            n1 /= 100;
         }

         // Rough initial estimate of square root.
         int k = 1;
         while (k < 10 && k * k < n1)
            k++;
         BigInteger root = (k > 1 ? k - 1 : 1) * BigInteger.Pow(10, pow);

         // Iterate to the desired value
         while (root * root > n || (root + 1) * (root + 1) <= n)
            root = (root + n / root) / 2;

         return root;
      }

      /// <summary>
      /// Calculates the square root of n mod p.
      /// </summary>
      /// <param name="n">The quadratic residue.</param>
      /// <param name="p">The prime modulus.</param>
      /// <returns>One of the square roots of n mod p.
      /// The other can be determined trivially by the caller.</returns>
      /// <remarks>
      /// <para>
      /// Reference: https://en.wikipedia.org/wiki/Tonelli–Shanks_algorithm
      /// </para>
      /// </remarks>
      public static long SquareRoot(BigInteger n, long p)
      {
         Assertions.True<ArgumentException>(Primes.IsPrime(p),
                  $"{nameof(p)} == {p} is not a prime.");
         Assertions.True<ArgumentException>(1 == JacobiSymbol(n, p),
                  $"{nameof(n)} == {n} is not a quadratic residue mod {p}.");

         if ((p & 3) == 3)
            return ModPow(n, (p + 1) / 4, p);

         // Find S & Q such that p - 1 == Q * 2**S
         int S = 0;
         long Q = p - 1;
         while ((Q & 1) == 0)
         {
            S++;
            Q >>= 1;
         }

         // Find a value of z that is a quadratic non-residue.
         long z = 2;
         while (1 == JacobiSymbol(z, p))
            z++;

         int M = S;
         long c = ModPow(z, Q, p);
         long t = ModPow(n, Q, p);
         long R = ModPow(n, (Q + 1) / 2, p);

         while (t != 0 && t != 1)
         {
            BigInteger t2 = t;
            int i = 0;
            while (t2 != 1)
            {
               t2 = (t2 * t2) % p;
               i++;
            }

            long b = ModPow(c, 1 << (M - i - 1), p);
            M = i;
            c = (b * b) % p;
            t = (t * c) % p;
            R = (R * b) % p;
         }

         return t == 0 ? 0 : R;
      }

      /// <summary>
      /// Calculates the y'th root of n.
      /// </summary>
      /// <param name="n"></param>
      /// <param name="y"></param>
      /// <returns>The floor of the y'th root of n.</returns>
      public static BigInteger Root(BigInteger n, int y)
      {
         // Estimate rough power of 2 of the root.
         int bit = 1 << y;  // 2 ** y;
         int pow = 0;
         BigInteger n1 = n;
         while (n1 > bit)
         {
            pow++;
            n1 >>= y;
         }

         BigInteger root = 1 << pow;

         // Iterate to the desired value
         while (Pow(root, y) > n || Pow(root + 1, y) <= n)
            root = ((y - 1) * root + n / Pow(root, y - 1)) / y;

         return root;
      }
   }
}
