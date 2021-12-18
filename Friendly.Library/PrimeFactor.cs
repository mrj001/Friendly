using System;

namespace Friendly.Library
{
   public class PrimeFactor
   {
      private readonly long _factor;
      private int _exponent;

      public PrimeFactor(long factor, int exponent)
      {
#if DEBUG
         if (!Primes.IsPrime(factor))
            throw new ArgumentException($"{nameof(factor)} must be a prime number");
#endif
         _factor = factor;
         _exponent = exponent;
      }

      public long Factor { get => _factor; }

      public int Exponent { get => _exponent; }

      #region object overrides
      public override string ToString()
      {
         return $"{_factor}**{_exponent}";
      }
      #endregion
   }
}
