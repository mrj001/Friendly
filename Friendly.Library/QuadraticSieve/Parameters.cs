using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class Parameters : IParameters
   {
      private readonly ParameterTableEntry[] _parameters = new ParameterTableEntry[]
      {
         // These are the values from Table 1 of the Silverman paper (Ref. B).
         new ParameterTableEntry(24,  100,   5_000, 1.5),
         new ParameterTableEntry(30,  200,  25_000, 1.5),
         new ParameterTableEntry(36,  400,  25_000, 1.75),
         new ParameterTableEntry(42,  900,  50_000, 2.0),
         new ParameterTableEntry(48, 1200, 100_000, 2.0),
         new ParameterTableEntry(54, 2000, 250_000, 2.2),
         new ParameterTableEntry(60, 3000, 350_000, 2.4),
         new ParameterTableEntry(66, 4500, 500_000, 2.6)
      };

      private int GetIndex(int numDigits)
      {
         int rv = 0;
         while (rv < _parameters.Length && _parameters[rv].Digits < numDigits)
            rv++;
         return rv < _parameters.Length ? rv : _parameters.Length - 1;
      }

      /// <inheritdoc/>
      public int FindSizeOfFactorBase(BigInteger kn)
      {
         int numDigits = BigIntegerCalculator.GetNumberOfDigits(kn);
         int index = GetIndex(numDigits);
         return _parameters[index].FactorBaseSize;
      }

      /// <inheritdoc/>
      public int FindSieveInterval(BigInteger kn)
      {
         int numDigits = BigIntegerCalculator.GetNumberOfDigits(kn);
         int index = GetIndex(numDigits);
         return _parameters[index].SieveInterval;
      }

      /// <inheritdoc/>
      public double FindLargePrimeTolerance(BigInteger kn)
      {
         int numDigits = BigIntegerCalculator.GetNumberOfDigits(kn);
         int index = GetIndex(numDigits);
         return _parameters[index].LargePrimeTolerance;
      }

      private class ParameterTableEntry
      {
         public ParameterTableEntry(int numDigits, int factorBaseSize,
            int sieveInterval, double largePrimeTolerance)
         {
            Digits = numDigits;
            FactorBaseSize = factorBaseSize;
            SieveInterval = sieveInterval;
            LargePrimeTolerance = largePrimeTolerance;
         }

         public int Digits { get; }
         public int FactorBaseSize { get; }
         public int SieveInterval { get; }
         public double LargePrimeTolerance { get; }
      }
   }
}

