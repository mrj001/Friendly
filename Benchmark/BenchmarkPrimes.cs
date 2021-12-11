using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Friendly.Library;

namespace Benchmark
{
   // Q: what maps to OverheadWarmup?  To OverheadActual? To WorkloadResult (in AfterActualRun)
   // Q: what value is there in "AfterActualRun"?
   // warmupCount maps to "WorkloadWarmup" in the output.
   // targetCount maps to "WorkloadActual" in the output.
   [SimpleJob(RunStrategy.Throughput, warmupCount: 1, targetCount: 6, invocationCount: 1)]
   public class BenchmarkPrimes
   {
      public BenchmarkPrimes()
      {
      }

      //1_073_741_824
      [Params(1_048_576, 4_194_304, 16_777_216, 67_108_864)]
      public long Count { get; set; }

      [Benchmark]
      public void RunSieve()
      {
         Primes.Init(Count);
      }
   }
}
