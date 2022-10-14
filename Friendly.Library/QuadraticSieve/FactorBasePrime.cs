using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class FactorBasePrime
   {
      private readonly int _prime;

      private int _rootNModP = -1;

      public FactorBasePrime(int prime)
      {
         _prime = prime;
      }

      public int Prime { get => _prime; }

      public void InitRootN(int rootNModP)
      {
         _rootNModP = rootNModP;
      }

      public int RootNModP { get => _rootNModP; }
   }
}

