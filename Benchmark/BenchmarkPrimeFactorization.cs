using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Friendly.Library;

namespace Benchmark
{
   [SimpleJob(RunStrategy.Throughput, warmupCount: 1, targetCount: 6, invocationCount: 1)]
   public class BenchmarkPrimeFactorization
   {
      public BenchmarkPrimeFactorization()
      {
         if (Primes.SieveLimit == 0)
            Primes.Init(20_000);  // must be at least sqrt(StartValue)
      }

      [Params(40_000_000)]
      public long StartValue { get; set; }

      [Benchmark]
      public void RunFactorize()
      {
         PrimeFactorization pf;
         long jul = StartValue + 1_000_000;
         for (long j = StartValue; j < jul; j++)
            pf = PrimeFactorization.Get(j);
      }
   }

}
