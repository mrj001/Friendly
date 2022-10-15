using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class FactorBasePrime
   {
      private readonly int _prime;
      private readonly float _log;

      private int _rootNModP = -1;

      public FactorBasePrime(int prime)
      {
         _prime = prime;
         _log = (float)Math.Log(_prime);
      }

      public int Prime { get => _prime; }

      public void InitRootN(int rootNModP)
      {
         _rootNModP = rootNModP;
      }

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

