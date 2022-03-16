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

   }
}
