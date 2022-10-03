using System;
using System.Numerics;
namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// Represents a polynomial of the form: a*(x**2) + 2*b*x + c
   /// </summary>
   public class Polynomial
   {
      private readonly long _a;
      private readonly long _b;
      private readonly BigInteger _c;
      private readonly BigInteger _inv2d;

      internal Polynomial(long a, long b, BigInteger c, BigInteger inv2d)
      {
         _a = a;
         _b = b;
         _c = c;
         _inv2d = inv2d;
      }

      public long A { get => _a; }
      public long B { get => _b; }

      /// <summary>
      /// Evaluates the full polynomial
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      public BigInteger Evaluate(long x)
      {
         BigInteger rv = _a * x;
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
      public BigInteger EvaluateLHS(long x)
      {
         BigInteger rv = 2 * _a * x + _b;
         rv *= _inv2d;
         return rv;
      }
   }
}
