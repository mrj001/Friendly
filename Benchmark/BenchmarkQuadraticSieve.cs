using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;

namespace Benchmark
{
   [SimpleJob(RunStrategy.Throughput)]
   public class BenchmarkQuadraticSieve
   {
      public BenchmarkQuadraticSieve()
      {
         // This limit must be at least as big as any factor in _factors.
         long limit = 2_147_483_648;
         if (Primes.SieveLimit < limit)
            Primes.Init(limit);
      }

      private static (long, long)[] _factors = new (long, long)[]
      {
         (10_247,10_267),
         (105_199, 101_419),
         (1_046_659, 1_001_387),
         (10_240_033, 10_185_761),
         (106_524_623, 100_729_339),
         (1_092_547_727, 1_066_246_453)
      };

      [Params(0,1,2,3,4,5)]
      public int FactorIndex { get; set; }

      [Benchmark]
      public void RunQuadraticSieve()
      {
         (long f1, long f2) = _factors[FactorIndex];
         if (f1 > f2)
         {
            long t = f1;
            f1 = f2;
            f2 = t;
         }

         long n = f1 * f2;
         (long g1, long g2) = QuadraticSieve.Factor(n);

         if (g1 > g2)
         {
            long t = g1;
            g1 = g2;
            g2 = t;
         }

         if (g1 != f1 || g2 != f2)
            throw new ApplicationException();
      }

      [Benchmark]
      public void RunTrialDivision()
      {
         // Since the Quadratic Sieve only finds two factors, we'll stop this
         // after finding one factor.  The benchmark runs with only two factors
         // anyway.
         (long f1, long f2) = _factors[FactorIndex];
         if (f1 > f2)
         {
            long t = f1;
            f1 = f2;
            f2 = t;
         }
         long product = f1 * f2;

         long g1, g2, r;
         IEnumerator<long> j = Primes.GetEnumerator();
         j.MoveNext();
         do
         {
            g1 = j.Current;
            g2 = Math.DivRem(product, g1, out r);
         } while (j.MoveNext() && r != 0);

         if (r != 0 || g1 != f1 || g2 != f2)
            throw new ApplicationException();
      }
   }
}
