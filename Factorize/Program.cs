using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Friendly.Library;

namespace Factorize
{
   class Program
   {
      static void Main(string[] args)
      {
         long n;
         if (!long.TryParse(args[args.Length - 1], NumberStyles.Integer | NumberStyles.AllowThousands, null, out n))
         {
            Console.WriteLine($"Unable to parse {args[args.Length - 1]}");
            return;
         }

         Stopwatch watch = Stopwatch.StartNew();
         Primes.Init(1 + LongCalculator.SquareRoot(n));  // sufficient for the given number
         TimeSpan sieveTime = watch.Elapsed;
         PrimeFactorization factors = PrimeFactorization.Get(n);
         TimeSpan factorTime = watch.Elapsed - sieveTime;

         // Determine the number of characters needed to display the largest factor
         // and the largest exponent.
         int factorWidth = 0;
         int powerWidth = 0;
         foreach (PrimeFactor f in factors)
         {
            factorWidth = (int)Math.Max(Math.Ceiling(BigInteger.Log10(f.Factor)), factorWidth);
            powerWidth = (int)Math.Max(Math.Ceiling(Math.Log10(f.Exponent)), powerWidth);
         }

         // Add some space to allow for comma separators
         factorWidth += (factorWidth - 1) / 3;
         powerWidth += (powerWidth - 1) / 3;

         Console.WriteLine($"The prime factors of {n} are:");
         string fmt = $"{{0, {factorWidth}}} {{1, {powerWidth}}}";
         Console.WriteLine(fmt, "Factor", "Power");
         fmt = $"{{0,{factorWidth}:N0}} {{1,{powerWidth}:N0}}";
         foreach (PrimeFactor f in factors)
            Console.WriteLine(fmt, f.Factor, f.Exponent);

         Console.WriteLine($"Sieve Time:     {sieveTime:mm\\:ss\\.ffff}");
         Console.WriteLine($"Factoring Time: {factorTime:mm\\:ss\\.ffff}");
      }
   }
}
