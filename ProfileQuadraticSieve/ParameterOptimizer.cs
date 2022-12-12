using System;
using System.Numerics;
using Friendly.Library;
using Friendly.Library.Logging;
using Friendly.Library.QuadraticSieve;

namespace ProfileQuadraticSieve
{
   public enum ParameterToOptimize
   {
      SizeOfFactorBase = 1,
      SieveInterval = 2,
      LargePrimeTolerance = 3,
      SmallPrimeLimit = 4
   }

   public static class ParameterOptimizer
   {
      public static void Optimize(ParameterToOptimize parameter, int numDigits,
         int repeats, double minValue, double maxValue, double step)
      {
         Console.WriteLine("Sieving...");
         Primes.Init(2_147_483_648);

         OptimizableParameter parameters = new OptimizableParameter(parameter);
         parameters.CurrentParameter = minValue;

         double totalSeconds;
         double minSeconds;
         double maxSeconds;
         int totalPolynomials;
         int actualRepeats;

         FileLogger? resultLogger = null;
         try
         {
            resultLogger = new FileLogger("results.log");
            resultLogger.WriteLine($"Digits:\t{numDigits}");
            resultLogger.WriteLine($"Optimizing:\t{parameter}");
            resultLogger.WriteLine("Value\tRepetitions\tTotalSeconds\tMinimum\tMaximum\tPolynomials");

            while (parameters.CurrentParameter <= maxValue &&
               (parameter != ParameterToOptimize.SmallPrimeLimit || Primes.IsPrime((int)parameters.CurrentParameter)))
            {
               actualRepeats = repeats;

               Program.RunOptimizations(parameters, 3600, numDigits, ref actualRepeats,
                  out totalSeconds, out minSeconds, out maxSeconds, out totalPolynomials);

               resultLogger.WriteLine($"{parameters.CurrentParameter:0.######}\t{actualRepeats}\t{totalSeconds:0.######}\t{minSeconds:0.######}\t{maxSeconds:0.######}\t{totalPolynomials}\t");
               resultLogger.Flush();

               if (parameter != ParameterToOptimize.SmallPrimeLimit)
               {
                  parameters.CurrentParameter += step;
               }
               else
               {
                  do
                  {
                     parameters.CurrentParameter += 1;
                  } while (parameters.CurrentParameter < maxValue && !Primes.IsPrime((int)parameters.CurrentParameter));
               }
            }
         }
         finally
         {
            resultLogger?.Dispose();
            resultLogger = null;
         }
      }

      private class OptimizableParameter : IParameters
      {
         private readonly IParameters _parameters = new Parameters();

         private double _curParameter;

         private readonly ParameterToOptimize _optimizing;

         public OptimizableParameter(ParameterToOptimize optimizing)
         {
            _optimizing = optimizing;
         }

         public double CurrentParameter
         {
            get
            {
               return _curParameter;
            }
            set
            {
               _curParameter = value;
            }
         }

         public int FindSizeOfFactorBase(BigInteger n)
         {
            if (_optimizing == ParameterToOptimize.SizeOfFactorBase)
               return (int)_curParameter;
            else
               return _parameters.FindSizeOfFactorBase(n);
         }

         public int FindSieveInterval(BigInteger n)
         {
            if (_optimizing == ParameterToOptimize.SieveInterval)
               return (int)_curParameter;
            else
               return _parameters.FindSieveInterval(n);
         }

         public double FindLargePrimeTolerance(BigInteger n)
         {
            if (_optimizing == ParameterToOptimize.LargePrimeTolerance)
               return _curParameter;
            else
               return _parameters.FindLargePrimeTolerance(n);
         }

         public int FindSmallPrimeLimit(BigInteger n)
         {
            if (_optimizing == ParameterToOptimize.SmallPrimeLimit)
               return (int)_curParameter;
            else
               return _parameters.FindSmallPrimeLimit(n);
         }

         public IRelationsFactory GetRelationsFactory()
         {
            return new RelationsFactory();
         }
      }
   }
}

