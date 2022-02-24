using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Friendly.Library.Pollard;

namespace Benchmark
{
   [SimpleJob(RunStrategy.Throughput)]
   public class BenchmarkPollardRho
   {
      public BenchmarkPollardRho()
      {
      }

      private static (BigInteger, BigInteger)[] _factors = new (BigInteger, BigInteger)[]
      {
         (10_247,10_267),
         (101_419, 105_199),
         (1_001_387, 1_046_659),
         (10_185_761, 10_240_033),
         (100_729_339, 106_524_623),
         (1_066_246_453, 1_092_547_727),
         (10_870_149_781, 10_936_614_389)
      };

      [Params(0, 1, 2, 3, 4, 5, 6)]
      public int FactorIndex { get; set; }

      [Benchmark]
      public void RunPollardRho()
      {
         (BigInteger f1, BigInteger f2) = _factors[FactorIndex];
         if (f1 > f2)
         {
            BigInteger t = f1;
            f1 = f2;
            f2 = t;
         }

         BigInteger n = f1 * f2;
         PollardRho rho = new PollardRho();
         (BigInteger g1, BigInteger g2) = rho.Factor(f1 * f2);

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
