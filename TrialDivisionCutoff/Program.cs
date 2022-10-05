using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Friendly.Library;

namespace TrialDivisionCutoff
{
   public class Program
   {
      static void Main(string[] args)
      {
         // The last value is chosen to cause ONLY trial division to be used.
         long[] highestDivisors = new long[] { 97, 199, 293, 397, 499, 599, 691, 797, 887, 997, 500_000 };
         long jll = 168_930_000_000;
         long jul = jll + 10_000_000;
         Stopwatch sw = new Stopwatch();
         string fmtTimeStamp = "hh\\:mm\\:ss\\.fff";

         Console.WriteLine($"{DateTime.Now.ToString(fmtTimeStamp)}: Sieving");
         Primes.Init(highestDivisors.Max());

         using (TextWriter tw = new StreamWriter("TrialDivision.cvs", false))
            tw.WriteLine("Highest Divisor,Elapsed Time,Rho Invocations");

         for (int k = 0; k < highestDivisors.Length; k++)
         {
            Console.WriteLine($"{DateTime.Now.ToString(fmtTimeStamp)}: Highest Divisor: {highestDivisors[k]}");
            PrimeFactorization.HighestTrialDivisor = highestDivisors[k];
            int rhoInvocations = 0;
            sw.Restart();
            for (long j = jll; j < jul; j++)
            {
               if ((j % 1_000_000) == 0)
                  Console.WriteLine($"{DateTime.Now.ToString(fmtTimeStamp)}: j: {j}");
               PrimeFactorization pf = PrimeFactorization.Get(j);
               if (pf.Number != j)
                  throw new ApplicationException($"Failure at j = {j}");
               rhoInvocations += pf.RhoInvocations;
            }

            TimeSpan elapsed = sw.Elapsed;
            using (TextWriter tw = new StreamWriter("TrialDivision.cvs", true))
               tw.WriteLine($"{highestDivisors[k]},{elapsed},{rhoInvocations}");
         }
      }
   }
}
