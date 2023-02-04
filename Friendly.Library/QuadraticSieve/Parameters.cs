using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// Specifies how many Large Primes to use when finding Relations.
   /// </summary>
   public enum LargePrimeStrategy
   {
      OneLargePrime = 1,
      TwoLargePrimes = 2,
      ThreeLargePrimes = 3
   }

   public class Parameters : IParameters
   {
      private readonly ParameterTableEntry[] _parameters = new ParameterTableEntry[]
      {
         // These values were initially from Table 1 of the Silverman paper (Ref. B).
         // I've added the small prime cutoff, LargePrimeStategy,
         // and altered some for optimization.
         new ParameterTableEntry(24,  100,   5_000, 1.5,  3, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(30,  200,  25_000, 1.5,  5, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(36,  400,  25_000, 1.61, 5, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(42,  900,  50_000, 1.75, 5, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(48, 1200, 100_000, 2.07, 7, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(54, 2000, 250_000, 2.2, 11, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(60, 3000, 350_000, 2.22, 17, LargePrimeStrategy.OneLargePrime),
         new ParameterTableEntry(66, 6000, 400_000, 2.35, 17, LargePrimeStrategy.TwoLargePrimes),
         // Values below this line were not present in Ref. B.
         new ParameterTableEntry(72,  8_500, 500_000, 2.35, 17, LargePrimeStrategy.TwoLargePrimes),
         new ParameterTableEntry(78, 13_750, 600_000, 2.35, 17, LargePrimeStrategy.TwoLargePrimes),
         // wild guesses below here.
         new ParameterTableEntry(84,  15_000,   700_000, 3.2, 17, LargePrimeStrategy.ThreeLargePrimes),
         new ParameterTableEntry(90,  20_000,   800_000, 3.2, 17, LargePrimeStrategy.ThreeLargePrimes),
         new ParameterTableEntry(96,  25_000,   900_000, 3.2, 17, LargePrimeStrategy.ThreeLargePrimes),
         new ParameterTableEntry(102, 30_000, 1_000_000, 3.2, 17, LargePrimeStrategy.ThreeLargePrimes)
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

      /// <inheritdoc/>
      public LargePrimeStrategy FindLargePrimeStrategy(BigInteger n)
      {
         int numDigits = BigIntegerCalculator.GetNumberOfDigits(n);
         int index = GetIndex(numDigits);
         return _parameters[index].LargePrimeStrategy;
      }

      /// <inheritdoc/>
      public IRelationsFactory GetRelationsFactory()
      {
         return new RelationsFactory();
      }

      /// <inheritdoc/>
      public int MaxDegreeOfParallelism()
      {
         return Environment.ProcessorCount / 2;
      }

      private class ParameterTableEntry
      {
         public ParameterTableEntry(int numDigits, int factorBaseSize,
            int sieveInterval, double largePrimeTolerance, int smallPrimeLimit,
            LargePrimeStrategy largePrimeStrategy)
         {
            Digits = numDigits;
            FactorBaseSize = factorBaseSize;
            SieveInterval = sieveInterval;
            LargePrimeTolerance = largePrimeTolerance;
            SmallPrimeLimit = smallPrimeLimit;
            LargePrimeStrategy = largePrimeStrategy;
         }

         public int Digits { get; }
         public int FactorBaseSize { get; }
         public int SieveInterval { get; }
         public double LargePrimeTolerance { get; }
         public int SmallPrimeLimit { get; }
         public LargePrimeStrategy LargePrimeStrategy { get; }
      }
   }
}

