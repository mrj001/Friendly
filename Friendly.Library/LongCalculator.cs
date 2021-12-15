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
   }
}
