using System;
using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Friendly.Library;

namespace Benchmark
{
   public class BenchmarkGCD
   {
      // 65_537 is the 6543rd prime number.
      private const int lowPrimeIndex = 6543;

      // There are 9592 primes below 100_000.
      private const int hiPrimeIndex = 9592;

      private const int limit = 1000;
      private BigInteger[] a = new BigInteger[limit];
      private BigInteger[] b = new BigInteger[limit];
      private BigInteger[] gcd = new BigInteger[limit];

      public BenchmarkGCD()
      {
         if (Primes.SieveLimit < 100_000)
            Primes.Init(100_000);

         List<int> primes = new List<int>(hiPrimeIndex);
         IEnumerator<long> p = Primes.GetEnumerator();
         while (p.MoveNext())
            primes.Add((int)p.Current);

         int factor;
         Random rnd = new Random(123);
         for (int j = 0; j < limit; j ++)
         {
            // by choosing 4 to 7 prime factors > 2**16,
            // we ensure that all numbers passed to GCD are > 2**64.

            // Select factors of a
            int kul = rnd.Next(4, 7);
            List<int> aFactors = new List<int>();
            a[j] = BigInteger.One;
            for (int k = 0; k < kul; k ++)
            {
               factor = primes[rnd.Next(lowPrimeIndex, hiPrimeIndex)];
               a[j] *= factor;
               aFactors.Add(factor);
            }

            // Select factors of b
            kul = rnd.Next(4, 7);
            List<int> bFactors = new List<int>();
            b[j] = BigInteger.One;
            while (bFactors.Count < kul)
            {
               do
               {
                  factor = primes[rnd.Next(lowPrimeIndex, hiPrimeIndex)];
               } while (aFactors.Contains(factor));
               b[j] *= factor;
               bFactors.Add(factor);
            }

            // Select a random prime (not a factor of a or b) to be the GCD
            do
            {
               factor = primes[rnd.Next(1, primes.Count)];
            } while (aFactors.Contains(factor) || bFactors.Contains(factor));
            gcd[j] = factor;
         }
      }

      [Benchmark]
      public void RunGCD()
      {
         for (int j = 0; j < limit; j ++)
         {
            BigInteger answer = BigIntegerCalculator.GCD(a[j], b[j]);
            Assertions.Equals(gcd[j], answer);
         }
      }
   }
}
