using System;
using System.Diagnostics;
using System.Numerics;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;

namespace ProfileQuadraticSieve
{
   public class Program
   {
      public static void Main(string[] args)
      {
         Console.WriteLine("1. Factoring a 30-digit number.");
         Console.WriteLine("2. Factoring a 38-digit number.");
         Console.WriteLine("3. Factor Fermat #7.");
         Console.WriteLine("4. Factor the product of 2 random primes.");
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

         switch(choice)
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

         Random rng = new Random(1234);
         double seconds;
         double totalSeconds = 0;
         double minSeconds = double.MaxValue;
         double maxSeconds = double.MinValue;

         for (int j = 0; j < repeats; j++)
         {
            Console.WriteLine($"Iteration: {j}");
            BigInteger f1 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2 + (numDigits & 1));
            BigInteger f2 = BigIntegerCalculator.RandomPrime(rng, numDigits / 2);
            seconds = Factor(f1, f2);
            totalSeconds += seconds;
            minSeconds = Math.Min(seconds, minSeconds);
            maxSeconds = Math.Max(seconds, maxSeconds);
         }
         Console.WriteLine("====  Summary  ====");
         Console.WriteLine($"Size of integers: {numDigits}");
         Console.WriteLine($"Number of iterations: {repeats}");
         Console.WriteLine($"Total time in seconds: {totalSeconds:0.000}");
         Console.WriteLine($"Average time per iteration: {totalSeconds / repeats:0.000}");
         Console.WriteLine($"Maximum iteration time: {maxSeconds:0.000}");
         Console.WriteLine($"Minimum iteration time: {minSeconds:0.000}");
      }

      private static Stopwatch? sw;

      /// <summary>
      /// Multiplies the two given (assumed) primes and then factors the produce
      /// using the Quadratic Sieve.
      /// </summary>
      /// <param name="f1"></param>
      /// <param name="f2"></param>
      /// <returns>The number of seconds taken to factor the product.</returns>
      private static double Factor(BigInteger f1, BigInteger f2)
      {
         double rv = -1;

         if (f1 > f2)
         {
            BigInteger tmp = f2;
            f2 = f1;
            f1 = tmp;
         }

         Console.WriteLine($"f1 = {f1}");
         Console.WriteLine($"f2 = {f2}");

         try
         {
            if (Primes.SieveLimit == 0)
            {
               Console.WriteLine("Sieving...");
               Primes.Init(2_147_483_648);
            }

            BigInteger n = f1 * f2;
            int numDigits = 1 + (int)Math.Floor(BigInteger.Log10(n));
            Console.WriteLine($"The number has {numDigits} digits.");

            sw = new();
            sw.Start();
            QuadraticSieve sieve = new(n);
            sieve.Progress += HandleProgress;
            (BigInteger g1, BigInteger g2) = sieve.Factor();

            if (g1 > g2)
            {
               BigInteger tmp = g2;
               g2 = g1;
               g1 = tmp;
            }

            // NOTE: This assumes only 2 prime factors.
            if (g1 != f1 || g2 != f2)
               Console.WriteLine("Incorrect factorization found");
            else
               Console.WriteLine("Correct Factors found.");

            Console.WriteLine($"Number of B-Smooth values found: {sieve.TotalBSmoothValuesFound}");
            Console.WriteLine($"Number of Polynomials used: {sieve.TotalPolynomials}");
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

         return rv;
      }

      private static void HandleProgress(object? sender, NotifyProgressEventArgs e)
      {
         Console.WriteLine($"{sw?.Elapsed:d\\:hh\\:mm\\:ss\\.ffff}: {e.Message}");
      }
   }
}