﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmark
{
   public class Program
   {
      public static void Main(string[] args)
      {
         var summary = BenchmarkRunner.Run<BenchmarkQuadraticSieve>();
      }
   }
}
