using System;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestPrimes
   {
      // The upper limit of the sieve is set to one less than a multiple of 64,
      // so we can use this constant in our tests.  This limit is inclusive
      // of the given value.
      private const int _upperLimit = 25_023;

      public TestPrimes()
      {
         Primes.Init(_upperLimit);
      }

      [Fact]
      public void IsPrime_Throws()
      {
#if DEBUG
         Assert.Throws<ArgumentOutOfRangeException>(() => Primes.IsPrime(-1));
#endif
         // Note: _upperLimit is inclusive, so we have to add one to get it
         // to throw the Exception.
         Assert.Throws<ArgumentOutOfRangeException>(() => Primes.IsPrime(_upperLimit + 1));
      }

      public static TheoryData<bool, long> IsPrimeTestData
      {
         get
         {
            var rv = new TheoryData<bool, long>();

            rv.Add(true, 2);
            rv.Add(true, 3);
            rv.Add(false, 49);
            rv.Add(false, 121);
            rv.Add(true, 4999);
            rv.Add(false, 24287);  // 149 * 163
            rv.Add(true, 25013);
            rv.Add(false, 25022);
            rv.Add(false, 25023);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(IsPrimeTestData))]
      public void IsPrime(bool expected, long value)
      {
         bool actual = Primes.IsPrime(value);

         Assert.Equal(expected, actual);
      }

      [Fact]
      public void SieveLimit()
      {
         // The non-inclusive SieveLimit is the next higher multiple of 64.
         long expected = 64 * ((_upperLimit + 63) / 64);
         Assert.Equal(expected, Primes.SieveLimit);
      }

      public static TheoryData<bool, long> IsPrimeFastTestData
      {
         get
         {
            var rv = new TheoryData<bool, long>();

            rv.Add(true, 2);
            rv.Add(true, 3);
            rv.Add(false, 49);
            rv.Add(false, 121);
            rv.Add(true, 4999);
            rv.Add(false, 24287);  // 149 * 163
            rv.Add(true, 25013);
            rv.Add(false, 25022);
            rv.Add(false, 25023);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(IsPrimeFastTestData))]
      public void IsPrimeFast(bool expected, long value)
      {
         bool actual = Primes.IsPrime(value);

         Assert.Equal(expected, actual);
      }

      [Fact]
      public void IsPrimeFast_Throws()
      {
         long outOfBounds = 64 * ((_upperLimit + 63) / 64);

         // This must not throw
         bool b = Primes.IsPrimeFast(outOfBounds - 1);

         Assert.Throws<ArgumentException>(() => Primes.IsPrimeFast(outOfBounds));
      }

      public static TheoryData<bool, long> MillerRabinTestData
      {
         get
         {
            var rv = new TheoryData<bool, long>();

            rv.Add(true, 4999);    // Just a prime number
            rv.Add(false, 65536);  // a power of two
            rv.Add(true, 65537);   // a prime that is one greater than a power of two
            rv.Add(false, 341);    // a Fermat pseudo prime to base 2.
            rv.Add(false, 29341);  // a Fermat pseudo-prime to bases 2 through 12.
            rv.Add(false, 1729);   // 1729 == 7*13*9; A Carmichael number
            rv.Add(false, 2047);   // 2047 == 23 * 89; a strong pseudo-prime to base 2.

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(MillerRabinTestData))]
      public void MillerRabin(bool expected, long n)
      {
         bool actual = Primes.MillerRabin(n);

         Assert.Equal(expected, actual);
      }

      [Fact]
      public void MillerRabin_Throws()
      {
#if DEBUG
         // Throws on even argument.
         Assert.Throws<ArgumentException>(() => Primes.MillerRabin(42));
#endif
      }
   }
}
