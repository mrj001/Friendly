using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public struct FactorBasePrime
   {
      private readonly int _prime;
      private readonly float _log;
      private readonly int _rootNModP = -1;

      public FactorBasePrime(int prime, int rootNModP)
      {
         _prime = prime;
         _log = (float)Math.Log(_prime);
         _rootNModP = rootNModP;
      }

      public int Prime { get => _prime; }

      public int RootNModP { get => _rootNModP; }

      /// <summary>
      /// Gets the natural logarithm of the FactorBasePrime.
      /// </summary>
      public float Log { get => _log; }

      public override string ToString()
      {
         return $"{_prime}";
      }
   }
}

