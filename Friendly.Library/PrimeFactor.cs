using System;
using System.Numerics;

namespace Friendly.Library
{
   public class PrimeFactor : IPrimeFactor
   {
      private readonly BigInteger _factor;
      private int _exponent;

      public PrimeFactor(BigInteger factor, int exponent)
      {
#if DEBUG
         if (!Primes.IsPrime(factor))
            throw new ArgumentException($"{nameof(factor)} must be a prime number");
#endif
         _factor = factor;
         _exponent = exponent;
      }

      public BigInteger Factor { get => _factor; }

      public int Exponent { get => _exponent; }

      #region object overrides
      public override string ToString()
      {
         return $"{_factor}**{_exponent}";
      }
      #endregion
   }
}
