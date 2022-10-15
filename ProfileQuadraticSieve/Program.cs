using System;
using System.Diagnostics;
using System.Numerics;
using Friendly.Library;
using Friendly.Library.Logging;
using Friendly.Library.QuadraticSieve;

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
               Factor(f1, f2);
               break;

            case 2:
               f1 = 5_626_527_389_734_197_521;
               f2 = 9_631_131_476_434_794_037;
               Factor(f1, f2);
               break;

            case 3:  // Start with the known factors of F7
               f1 = 59_649_589_127_497_217;
               f2 = BigInteger.Parse("5704689200685129054721");
               Factor(f1, f2);
               break;

            case 4:
               DoRandom();
               break;

            case 5:
               DoPerformanceTest();
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

         RunFactorings(double.MaxValue, numDigits, ref repeats, out totalSeconds,
            out minSeconds, out maxSeconds, out polyCount);

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
         int[] repeats = new int[] { 50, 50, 50, 50, 50, 50, 40, 25 };
         int repeatIndex = 0;
         Stopwatch swTotalTime = new();

         ILog origProgressLogger = _progressLogger;
         FileLogger? resultLogger = null;
         try
         {
            _progressLogger = new FileLogger("progress.log");
            resultLogger = new FileLogger("results.log");
            resultLogger.WriteLine("Digits\tRepetitions\tTotalSeconds\tMinimum\tMaximum\tPolynomials");

            swTotalTime.Start();
            for (int numDigits = 24; numDigits <= 66; numDigits += 6)
            {
               Random rng = new Random(1234);
               double iterationSeconds;
               double minSeconds;
               double maxSeconds;
               int polyCount;

               RunFactorings(_performanceTimeLimit.TotalSeconds - swTotalTime.Elapsed.TotalSeconds,
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

      private static void RunFactorings(double timeLimit, int numDigits,
         ref int repeats, out double totalSeconds,
         out double minSeconds, out double maxSeconds, out int totalPolynomials)
      {
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
            BigInteger f1 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2 + (numDigits & 1));
            BigInteger f2 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2);
            (seconds, polyCount) = Factor(f1, f2);
            totalSeconds += seconds;
            minSeconds = Math.Min(seconds, minSeconds);
            maxSeconds = Math.Max(seconds, maxSeconds);
            totalPolynomials += polyCount;
            if (totalSeconds > timeLimit)
               repeats = j + 1;
         }
      }

      private static Stopwatch? sw;

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
      private static (double, int) Factor(BigInteger f1, BigInteger f2)
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

            sw = new();
            sw.Start();
            QuadraticSieve sieve = new(n);
            sieve.Progress += HandleProgress;
            (BigInteger g1, BigInteger g2) = sieve.Factor();
            polyCount = sieve.TotalPolynomials;

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

            _progressLogger.WriteLine($"Number of B-Smooth values found: {sieve.TotalBSmoothValuesFound}");
            _progressLogger.WriteLine($"Number of Polynomials used: {sieve.TotalPolynomials}");
         }
         finally
         {
            if (sw is not null)
            {
               sw.Stop();
               rv = sw.Elapsed.TotalSeconds;
               sw = null;
            }
         }

         return (rv, polyCount);
      }

      private static void HandleProgress(object? sender, NotifyProgressEventArgs e)
      {
         _progressLogger?.WriteLine($"{sw?.Elapsed:d\\:hh\\:mm\\:ss\\.ffff}: {e.Message}");
      }
   }
}