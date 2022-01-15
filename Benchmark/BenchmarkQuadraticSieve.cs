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
         int limit = 10_000_000;
         if (Primes.SieveLimit < limit)
            Primes.Init(limit);
      }

      private static (long, long)[] _factors = new (long, long)[]
      {
         (1_046_659, 1_001_387),
         (10_240_033, 10_185_761),
         (106_524_623, 100_729_339),
         (1_092_547_727, 1_066_246_453)
      };

      [Params(3)]
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
   }
}
