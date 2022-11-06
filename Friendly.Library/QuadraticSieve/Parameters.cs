using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class Parameters : IParameters
   {
      private readonly ParameterTableEntry[] _parameters = new ParameterTableEntry[]
      {
         // These are the values from Table 1 of the Silverman paper (Ref. B).
         new ParameterTableEntry(24,  100,   5_000, 1.5, 5),
         new ParameterTableEntry(30,  200,  25_000, 1.5, 25),
         new ParameterTableEntry(36,  400,  25_000, 1.75, 25),
         new ParameterTableEntry(42,  900,  50_000, 2.0, 50),
         new ParameterTableEntry(48, 1200, 100_000, 2.0, 100),
         new ParameterTableEntry(54, 2000, 250_000, 2.2, 250),
         new ParameterTableEntry(60, 3000, 350_000, 2.4, 350),
         new ParameterTableEntry(66, 4500, 500_000, 2.6, 500)
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

      /// <inheritdoc/>
      public int FindSmallPrimeLimit(BigInteger kn)
      {
         int numDigits = BigIntegerCalculator.GetNumberOfDigits(kn);
         int index = GetIndex(numDigits);
         return _parameters[index].SmallPrimeLimit;
      }

      private class ParameterTableEntry
      {
         public ParameterTableEntry(int numDigits, int factorBaseSize,
            int sieveInterval, double largePrimeTolerance, int smallPrimeLimit)
         {
            Digits = numDigits;
            FactorBaseSize = factorBaseSize;
            SieveInterval = sieveInterval;
            LargePrimeTolerance = largePrimeTolerance;
            SmallPrimeLimit = smallPrimeLimit;
         }

         public int Digits { get; }
         public int FactorBaseSize { get; }
         public int SieveInterval { get; }
         public double LargePrimeTolerance { get; }
         public int SmallPrimeLimit { get; }
      }
   }
}

