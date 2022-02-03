using System;
namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// Represents a polynomial of the form: a*(x**2) + 2*b*x + c
   /// </summary>
   public class Polynomial
   {
      private readonly long _a;
      private readonly long _b;
      private readonly long _c;

      internal Polynomial(long a, long b, long c)
      {
         _a = a;
         _b = b;
         _c = c;
      }

      public long A { get => _a; }
      public long B { get => _b; }

      /// <summary>
      /// Evaluates the full polynomial
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      public long Evaluate(long x)
      {
         long rv = _a * x;
         rv += _b;
         rv *= x;
         rv += _c;

         return rv;
      }

      /// <summary>
      /// Evaluates the Left Hand Side of the relation: ax + b
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      public long EvaluateLHS(long x)
      {
         return _a * x + _b;
      }
   }
}
