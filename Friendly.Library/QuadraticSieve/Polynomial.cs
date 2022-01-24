using System;
namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// Represents a polynomial of the form: a * x**2 + 2b * x + c
   /// </summary>
   public class Polynomial
   {
      private readonly long _a;
      private readonly long _b;
      private readonly long _c;

      private Polynomial(long a, long b, long c)
      {
         _a = a;
         _b = b << 1;  // 2 * b
         _c = c;
      }

      public long A { get => _a; }
      public long B { get => _b >> 1; }
      public long C { get => _c; }

      public long Evaluate(long x)
      {
         long rv = _a * x;
         rv += _b;
         rv *= x;
         rv += _c;

         return rv;
      }
   }
}
