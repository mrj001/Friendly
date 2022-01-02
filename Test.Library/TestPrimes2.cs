using System;
using System.Collections.Generic;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestPrimes2
   {
      // Code inspection revealed a few problems with the Sieve of Eratosthenes
      // implementation.
      // The upper limit of the Sieve is inclusive.  The underlying BigBitArray
      // increases its capacity to the next highest multiple of 64 (non-inclusive)
      // Thus, the sieve limit is increased until it is congruent to 63 (mod 64).
      // Numbers between the passed in sieve upper limit and this increased
      // value were not properly sieved.  These tests catch that.

      // 2**16 == 65,536
      private static readonly long _upperLimit = 65_536;

      public TestPrimes2()
      {
         Primes.Init(_upperLimit);
      }

      [Fact]
      public void IsPrime1()
      {
         Assert.True(Primes.IsPrime(65_537));
         Assert.False(Primes.IsPrime(65_538));
      }

      [Fact]
      public void IsPrime2()
      {
         Assert.False(Primes.IsPrime(_upperLimit));
      }

      // According to:
      // http://sweet.ua.pt/tos/primes.html
      // The number of primes <= 2**16 is 6542
      [Fact]
      public void PrimeCount()
      {
         int primeCount = 0;
         IEnumerator<long> j = Primes.GetEnumerator();
         while (j.MoveNext() && j.Current <= _upperLimit)
            primeCount++;

         Assert.Equal(6542, primeCount);
      }
   }
}
