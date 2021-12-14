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
   }
}
