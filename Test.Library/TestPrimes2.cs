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
      // value were not properly sieved.  The worst case being when the
      // upper limit of the sieve was set to a multiple of 64.
      // These tests were created as a result.

      // 2**15 == 32,768
      private static readonly long _upperLimit = 32_768;

      public TestPrimes2()
      {
         Primes.Init(_upperLimit);
      }

      [Fact]
      public void IsPrime1()
      {
         Assert.True(Primes.IsPrime(32_771));
         Assert.False(Primes.IsPrime(32_772));
      }

      [Fact]
      public void IsPrime2()
      {
         Assert.False(Primes.IsPrime(_upperLimit));
      }

      // According to:
      // http://sweet.ua.pt/tos/primes.html
      // The number of primes <= 2**15 is 3512
      [Fact]
      public void PrimeCount()
      {
         int primeCount = 0;
         IEnumerator<long> j = Primes.GetEnumerator();
         while (j.MoveNext() && j.Current <= _upperLimit)
            primeCount++;

         Assert.Equal(3512, primeCount);
      }
   }
}
