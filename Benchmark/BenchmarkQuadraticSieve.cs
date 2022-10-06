using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;

namespace Benchmark
{
   // warmupCount maps to "WorkloadWarmup" in the output.
   // targetCount maps to "WorkloadActual" in the output.
   [SimpleJob(RunStrategy.Throughput, warmupCount: 2, targetCount: 10)]
   public class BenchmarkQuadraticSieve
   {
      public BenchmarkQuadraticSieve()
      {
         long limit = 2_147_483_648;
         if (Primes.SieveLimit < limit)
            Primes.Init(limit);
      }

      private static (BigInteger, BigInteger)[] _factors = new(BigInteger, BigInteger)[]
      {
         (10_247,10_267),
         (105_199, 101_419),
         (1_046_659, 1_001_387),
         (10_240_033, 10_185_761),
         (106_524_623, 100_729_339),
         (1_092_547_727, 1_066_246_453),             // 5; 10 digits
         (56_395_346_957, 28_433_673_481),           // 6; 11 digits
         (519_438_285_917, 881_984_581_069),         // 7; 12 digits
         (5_197_073_209_099, 3_875_368_659_733),     // 8; 13 digits
         (22_584_790_169_573, 50_816_358_827_621),   // 9; 14 digits
         (500_111_274_667_351, 299_456_570_077_393),              // 10; 15 digits
         (2_664_084_940_031_677, 4_297_751_019_632_717),          // 11; 16 digits
         (23_806_490_003_715_389, 19_987_728_972_900_803),        // 12; 17 digits
         (167_028_431_399_642_933, 328_627_472_829_101_449),      // 13; 18 digits
         (5_626_527_389_734_197_521, 9_631_131_476_434_794_037),  // 14; 19 digits
         //(BigInteger.Parse("73586197855458995167"), BigInteger.Parse("65387170920028618561")) // 20 digits
      };

      [Params(0,1,2,3,4,5, 6, 7, 8, 9, 10, 11, 12, 13, 14)]
      public int FactorIndex { get; set; }

      [Benchmark]
      public void RunQuadraticSieve()
      {
         (BigInteger f1, BigInteger f2) = _factors[FactorIndex];
         if (f1 > f2)
         {
            BigInteger t = f1;
            f1 = f2;
            f2 = t;
         }

         BigInteger n = f1 * f2;
         QuadraticSieve sieve = new QuadraticSieve(n);
         (BigInteger g1, BigInteger g2) = sieve.Factor();

         if (g1 > g2)
         {
            BigInteger t = g1;
            g1 = g2;
            g2 = t;
         }

         if (g1 != f1 || g2 != f2)
            throw new ApplicationException();
      }
   }
}
