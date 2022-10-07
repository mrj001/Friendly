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
               Console.WriteLine($"Exception caught at: {sw?.Elapsed:mm\\:ss\\.ffff}");
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
               break;

            case 2:
               f1 = 5_626_527_389_734_197_521;
               f2 = 9_631_131_476_434_794_037;
               break;

            default:
               throw new ApplicationException($"Unknown value for {nameof(choice)}: {choice}");
         }

         Factor(f1, f2);
      }

      private static Stopwatch? sw;

      private static void Factor(BigInteger f1, BigInteger f2)
      {
         if (f1 > f2)
         {
            BigInteger tmp = f2;
            f2 = f1;
            f1 = tmp;
         }

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
               sw = null;
            }
         }
      }

      private static void HandleProgress(object? sender, NotifyProgressEventArgs e)
      {
         Console.WriteLine($"{sw?.Elapsed:mm\\:ss\\.ffff}: {e.Message}");
      }
   }
}