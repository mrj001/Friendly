using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Friendly.Library;

namespace Friendly10
{
   public class Program
   {
      static void Main(string[] args)
      {
         Stopwatch watch = Stopwatch.StartNew();
         string fmtTimeStamp = "d\\.hh\\:mm\\:ss\\.fff";

         Console.WriteLine("Sieving");
         Primes.Init(4_294_967_296);
         Console.WriteLine("{0}: Sieving completed", watch.Elapsed.ToString(fmtTimeStamp));

         long target = 10;
         Fraction targetAbundancyIndex = AbundancyIndex(target);

         ParallelOptions options = new ParallelOptions();
         options.MaxDegreeOfParallelism = 2;
         options.CancellationToken = (new CancellationTokenSource()).Token;

         // In order for the Abundancy Index fraction to reduce to the same as
         // the target Abundancy Index, the numbers being checked must be a
         // multiple of the denominator.  Start the search at the next largest
         // multiple of the target denominator.
         long start = target + targetAbundancyIndex.Denominator - target % targetAbundancyIndex.Denominator;

         Parallel.ForEach<long>(Range(start, targetAbundancyIndex.Denominator, long.MaxValue),
            options, (j, state) =>
            {
            if (j % 10_000 == 0)
                  Console.WriteLine("{0}: Checking {1:N0}", watch.Elapsed.ToString(fmtTimeStamp), j);

               Fraction abundancyIndex = null;
               try
               {
                  abundancyIndex = AbundancyIndex(j);
               }
               catch (OverflowException ex)
               {
                  Console.WriteLine("{0}: Integer overflow exception at j == {1:N0}", watch.Elapsed.ToString(fmtTimeStamp), j);
                  Console.WriteLine(ex.StackTrace);
                  state.Break();
                  return;
               }

               if (abundancyIndex == targetAbundancyIndex)
               {
                  Console.WriteLine("{0}: found it: {1:N0}", watch.Elapsed.ToString(fmtTimeStamp), j);
                  state.Break();
                  return;
               }
            });

      }

      private static IEnumerable<long> Range(long start, long increment, long limit)
      {
         long n = start;
         while (n < limit)
         {
            yield return n;
            n += increment;
         }
      }

      private static Fraction AbundancyIndex(long n)
      {
         PrimeFactorization pf = PrimeFactorization.Get(n);
         return new Fraction(pf.SumOfFactors, n);
      }
   }
}
