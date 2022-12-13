using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using Friendly.Library;
using Friendly.Library.Logging;
using Friendly.Library.QuadraticSieve;
using Friendly.Library.Utility;

namespace ProfileQuadraticSieve
{
   public class Program
   {
      private static ILog _progressLogger = new ConsoleLogger();

      private static TimeSpan _performanceTimeLimit = TimeSpan.FromHours(8);

      public static void Main(string[] args)
      {
         Console.WriteLine("1. Factoring a 30-digit number.");
         Console.WriteLine("2. Factoring a 38-digit number.");
         Console.WriteLine("3. Factor Fermat #7.");
         Console.WriteLine("4. Factor the product of 2 random primes.");
         Console.WriteLine("5. Performance Test (long running).");
         Console.WriteLine("6. Parameter optimization (long running)");
         Console.WriteLine("7. Resume from save.");
         Console.Write("Enter number of choice: ");
         string? s = Console.ReadLine();
         int choice;
         if (int.TryParse(s, out choice))
         {
            try
            {
               DoChoice(choice);
            }
            catch (Exception ex)
            {
               Console.WriteLine($"Exception caught at: {sw?.Elapsed:d\\:hh\\:mm\\:ss\\.ffff}");
               Console.WriteLine(ex.Message);
               Console.WriteLine(ex.StackTrace);
            }
         }
         else
         {
            Console.WriteLine($"Invalid choice: '{s}'");
            return;
         }
      }

      private static void DoChoice(int choice)
      {
         BigInteger f1, f2;

         switch (choice)
         {
            case 1:
               f1 = 500_111_274_667_351;
               f2 = 299_456_570_077_393;
               Factor(new Parameters(), f1, f2);
               break;

            case 2:
               f1 = 5_626_527_389_734_197_521;
               f2 = 9_631_131_476_434_794_037;
               Factor(new Parameters(), f1, f2);
               break;

            case 3:  // Start with the known factors of F7
               f1 = 59_649_589_127_497_217;
               f2 = BigInteger.Parse("5704689200685129054721");
               Factor(new Parameters(), f1, f2);
               break;

            case 4:
               DoRandom();
               break;

            case 5:
               DoPerformanceTest();
               break;

            case 6:
               DoOptimizationTest();
               break;

            case 7:
               DoResume();
               break;

            default:
               throw new ApplicationException($"Unknown value for {nameof(choice)}: {choice}");
         }
      }

      private static void DoRandom()
      {
         int numDigits;
         string? s;
         do
         {
            Console.Write("Enter the number of digits of the product (>= 20): ");
            s = Console.ReadLine();
         } while (!int.TryParse(s, out numDigits));

         if (numDigits < 20)
         {
            Console.WriteLine("Too small a number");
            return;
         }

         int repeats;
         do
         {
            Console.Write("Enter the number of repetitions: ");
            s = Console.ReadLine();
         } while (!int.TryParse(s, out repeats));

         if (repeats < 1)
            return;

         double totalSeconds = 0;
         double minSeconds = double.MaxValue;
         double maxSeconds = double.MinValue;
         int polyCount = 0;

         IParameters parameters = new Parameters();
         RunFactorings(parameters, double.MaxValue, numDigits, ref repeats,
            out totalSeconds, out minSeconds, out maxSeconds, out polyCount);

         _progressLogger.WriteLine("====  Summary  ====");
         _progressLogger.WriteLine($"Size of integers: {numDigits}");
         _progressLogger.WriteLine($"Number of iterations: {repeats}");
         _progressLogger.WriteLine($"Total time in seconds: {totalSeconds:0.000}");
         _progressLogger.WriteLine($"Average time per iteration: {totalSeconds / repeats:0.000}");
         _progressLogger.WriteLine($"Maximum iteration time: {maxSeconds:0.000}");
         _progressLogger.WriteLine($"Minimum iteration time: {minSeconds:0.000}");
         _progressLogger.WriteLine($"Polynomials used: {polyCount}");
      }

      private static void DoPerformanceTest()
      {
         int[] repeats = new int[] { 50, 50, 50, 50, 50, 50, 40, 25, 25 };
         int repeatIndex = 0;
         Stopwatch swTotalTime = new();
         IParameters parameters = new Parameters();

         ILog origProgressLogger = _progressLogger;
         FileLogger? resultLogger = null;
         try
         {
            _progressLogger = new FileLogger("progress.log");
            resultLogger = new FileLogger("results.log");
            resultLogger.WriteLine("Digits\tRepetitions\tTotalSeconds\tMinimum\tMaximum\tPolynomials");

            swTotalTime.Start();
            for (int numDigits = 24; numDigits <= 72; numDigits += 6)
            {
               Random rng = new Random(1234);
               double iterationSeconds;
               double minSeconds;
               double maxSeconds;
               int polyCount;

               RunFactorings(parameters, _performanceTimeLimit.TotalSeconds - swTotalTime.Elapsed.TotalSeconds,
                  numDigits, ref repeats[repeatIndex], out iterationSeconds, out minSeconds, out maxSeconds,
                  out polyCount);

               resultLogger.WriteLine($"{numDigits}\t{repeats[repeatIndex]}\t{iterationSeconds}\t{minSeconds}\t{maxSeconds}\t{polyCount}");
               resultLogger.Flush();

               if (swTotalTime.Elapsed > _performanceTimeLimit)
               {
                  _progressLogger.WriteLine("Terminating: time limit exceeded.");
                  break;
               }
               if (swTotalTime.Elapsed.TotalSeconds + iterationSeconds / repeats[repeatIndex] > _performanceTimeLimit.TotalSeconds)
               {
                  _progressLogger.WriteLine("Terminating: next iteration expected to exceed time limit.");
                  break;
               }

               repeatIndex++;
            }
         }
         finally
         {
            (_progressLogger as FileLogger)?.Dispose();
            _progressLogger = origProgressLogger;
            resultLogger?.Dispose();
            resultLogger = null;
         }
      }

      private static void DoOptimizationTest()
      {
         Console.WriteLine("Choose parameter to optimize:");
         Console.WriteLine("1. Size of Factor Base");
         Console.WriteLine("2. Sieve Interval");
         Console.WriteLine("3. Large Prime Tolerance");
         Console.WriteLine("4. Small Prime Limit");
         string? s;
         int choice;
         do
         {
            s = Console.ReadLine();
         } while (!int.TryParse(s, out choice));
         if (choice < 1 || choice > 4)
            return;

         Console.Write("Number of digits: ");
         int numDigits;
         do
         {
            s = Console.ReadLine();
         } while (!int.TryParse(s, out numDigits));

         Console.Write("Enter the number of trials per parameter value: ");
         int repeats;
         do
         {
            s = Console.ReadLine();
         } while (!int.TryParse(s, out repeats));

         Console.Write("Enter the minimum parameter value: ");
         double minValue;
         do
         {
            s = Console.ReadLine();
         } while (!double.TryParse(s, out minValue));
         if (choice != 3 && minValue != Math.Round(minValue))
         {
            minValue = Math.Round(minValue);
            Console.WriteLine($"Rounding to {minValue}");
         }

         Console.Write("Enter the maximum parameter value: ");
         double maxValue;
         do
         {
            s = Console.ReadLine();
         } while (!double.TryParse(s, out maxValue));
         if (choice != 3 && maxValue != Math.Round(maxValue))
         {
            maxValue = Math.Round(maxValue);
            Console.WriteLine($"Rounding to {maxValue}");
         }

         double step = 1;
         if ((ParameterToOptimize)choice != ParameterToOptimize.SmallPrimeLimit)
         {
            Console.WriteLine("Enter the step value: ");
            do
            {
               s = Console.ReadLine();
            } while (!double.TryParse(s, out step));
            if (choice != 3 && step != Math.Round(step))
            {
               step = Math.Round(step);
               Console.WriteLine($"Rounding to {step}");
            }
         }

         ParameterOptimizer.Optimize((ParameterToOptimize)choice, numDigits, repeats, minValue, maxValue, step);
      }

      private static void DoResume()
      {
         Console.Write("Enter save file name: ");
         string? filename = Console.ReadLine();
         if (filename is null || !File.Exists(filename))
         {
            Console.WriteLine("File not found.");
            return;
         }

         if (Primes.SieveLimit == 0)
         {
            _progressLogger.WriteLine("Sieving...");
            Primes.Init(2_147_483_648);
         }

         QuadraticSieve sieve = new QuadraticSieve(new Parameters(), filename);
         sieve.Progress += HandleProgress;
         (BigInteger f1, BigInteger f2) = sieve.Factor();

         Console.WriteLine($"f1 = {f1}");
         Console.WriteLine($"f2 = {f2}");

         Statistic[] stats = sieve.GetStats();
         foreach (Statistic stat in stats)
            Console.WriteLine(stat);
      }

      public static void RunFactorings(IParameters parameters, double timeLimit, int numDigits,
         ref int repeats, out double totalSeconds,
         out double minSeconds, out double maxSeconds, out int totalPolynomials)
      {
         BigInteger f1, f2;
         Random rng = new Random(1234);
         double seconds;
         int polyCount;
         totalSeconds = 0;
         minSeconds = double.MaxValue;
         maxSeconds = double.MinValue;
         totalPolynomials = 0;

         for (int j = 0; j < repeats; j++)
         {
            _progressLogger.WriteLine($"Iteration: {j}");
            do
            {
               f1 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2 + (numDigits & 1));
               f2 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2);
            } while (BigIntegerCalculator.GetNumberOfDigits(f1 * f2) < numDigits);
            (seconds, polyCount) = Factor(parameters, f1, f2);
            totalSeconds += seconds;
            minSeconds = Math.Min(seconds, minSeconds);
            maxSeconds = Math.Max(seconds, maxSeconds);
            totalPolynomials += polyCount;
            if (totalSeconds > timeLimit)
               repeats = j + 1;
         }
      }

      public static void RunOptimizations(IParameters parameters, double timeLimit, int numDigits,
         ref int repeats, out double totalSeconds,
         out double minSeconds, out double maxSeconds, out int totalPolynomials)
      {
         Random rng = new Random(1234);
         BigInteger n;
         double seconds;
         totalSeconds = 0;
         minSeconds = double.MaxValue;
         maxSeconds = double.MinValue;
         totalPolynomials = 0;

         for (int j = 0; j < repeats; j++)
         {
            _progressLogger.WriteLine($"Iteration: {j}");
            do
            {
               BigInteger f1 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2 + (numDigits & 1));
               BigInteger f2 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2);
               n = f1 * f2;
            } while (BigIntegerCalculator.GetNumberOfDigits(n) < numDigits);

            QuadraticSieve sieve = new(parameters, n);
            sieve.Progress += HandleProgress;
            seconds = sieve.ParameterTest(500);

            totalSeconds += seconds;
            minSeconds = Math.Min(seconds, minSeconds);
            maxSeconds = Math.Max(seconds, maxSeconds);
            totalPolynomials += sieve.TotalPolynomials;
            if (totalSeconds > timeLimit)
               repeats = j + 1;
         }
      }

      private static Stopwatch? sw;
      private static Timer? _saveTimer;
      private static QuadraticSieve? _sieve;

      /// <summary>
      /// Multiplies the two given (assumed) primes and then factors the product
      /// using the Quadratic Sieve.
      /// </summary>
      /// <param name="logger"></param>
      /// <param name="f1"></param>
      /// <param name="f2"></param>
      /// <returns>
      /// <list type="number">
      /// <item>The number of seconds taken to factor the product.</item>
      /// <item>The number of polynomials used to factor the product.</item>
      /// </list>
      /// </returns>
      private static (double, int) Factor(IParameters parameters, BigInteger f1, BigInteger f2)
      {
         double rv = -1;
         int polyCount = 0;

         if (Primes.SieveLimit == 0)
         {
            _progressLogger.WriteLine("Sieving...");
            Primes.Init(2_147_483_648);
         }

         if (f1 > f2)
         {
            BigInteger tmp = f2;
            f2 = f1;
            f1 = tmp;
         }

         _progressLogger.WriteLine($"f1 = {f1}");
         _progressLogger.WriteLine($"f2 = {f2}");

         try
         {
            BigInteger n = f1 * f2;
            int numDigits = 1 + (int)Math.Floor(BigInteger.Log10(n));
            _progressLogger.WriteLine($"The number has {numDigits} digits.");

            _saveTimer = new Timer(HandleScheduledSave, null, TimeSpan.FromHours(2), TimeSpan.FromHours(2));
            Console.CancelKeyPress += HandleCancelSave;

            sw = new();
            sw.Start();
            _sieve = new(parameters, n);
            _sieve.Progress += HandleProgress;
            (BigInteger g1, BigInteger g2) = _sieve.Factor();

            if (g1 > g2)
            {
               BigInteger tmp = g2;
               g2 = g1;
               g1 = tmp;
            }

            // NOTE: This assumes only 2 prime factors.
            if (g1 != f1 || g2 != f2)
               _progressLogger.WriteLine("Incorrect factorization found");
            else
               _progressLogger.WriteLine("Correct Factors found.");

            _progressLogger.WriteLine("Statistics:");
            Statistic[] stats = _sieve.GetStats();
            foreach (Statistic stat in stats)
               _progressLogger.WriteLine(stat.ToString());
            polyCount = (int)stats.Where(s => s.Name == StatisticNames.TotalPolynomials).First().Value;
         }
         catch (AbortException)
         {
            Console.WriteLine("Factoring was aborted...");
         }
         finally
         {
            if (sw is not null)
            {
               sw.Stop();
               rv = sw.Elapsed.TotalSeconds;
               sw = null;
            }
            _saveTimer?.Dispose();
            _saveTimer = null;
            _sieve = null;
            Console.CancelKeyPress -= HandleCancelSave;
         }

         return (rv, polyCount);
      }

      private static void HandleScheduledSave(object? state)
      {
         string filename = GetSaveFileName();
         _sieve!.SaveState(SerializationReason.SaveState, filename);

      }

      private static void HandleCancelSave(object? sender, ConsoleCancelEventArgs e)
      {
         e.Cancel = true;
         Console.Error.WriteLine("Aborting...");
         string filename = GetSaveFileName();
         _sieve?.SaveState(SerializationReason.Shutdown, filename);
      }

      private static string GetSaveFileName()
      {
         return string.Format("Save-{0:yyyy-MM-dd_HH.mm}.xml.gz", DateTime.Now);
      }

      private static void HandleProgress(object? sender, NotifyProgressEventArgs e)
      {
         _progressLogger?.WriteLine(e.ToString());
      }
   }
}