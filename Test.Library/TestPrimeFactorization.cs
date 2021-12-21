using System;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestPrimeFactorization
   {
      public TestPrimeFactorization()
      {
         TestPrimes.EnsureInitialized();
      }

      public static TheoryData<(long factor, int exponent)[], long> GetPrimeFactorizationTestData
      {
         get
         {
            var rv = new TheoryData<(long factor, int exponent)[], long>();

            rv.Add(new (long, int)[] { (2, 2), (7, 1)}, 28);
            rv.Add(new (long, int)[] { (2, 4), (3, 1) }, 48);
            rv.Add(new (long, int)[] { (7, 2) }, 49);
            rv.Add(new (long, int)[] { (149, 2), (163, 2) }, 149 * 149 * 163 * 163);
            rv.Add(new (long, int)[] { (7, 1) , (11, 1), (13, 1), (60149, 1) }, 7 * 11 * 13 * 60149);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(GetPrimeFactorizationTestData))]
      public void GetPrimeFactorization((long factor, int exponent)[] expected, long n)
      {
         PrimeFactorization actual = PrimeFactorization.Get(n);

         Assert.Equal(expected.Length, actual.Count);

         for (int j = 0; j < expected.Length; j ++)
         {
            Assert.Equal(expected[j].factor, actual[j].Factor);
            Assert.Equal(expected[j].exponent, actual[j].Exponent);
         }
      }

      [Fact]
      public void GetPrimeFactorization_Throws()
      {
         // TODO when factorization of larger integers is implemented, this
         //   test needs to be removed.  This number has 2 prime factors larger
         //   than the Sieve Limit (See TestPrimes._upperLimit).
         //   Note that the product of these factors must be less than 55108**2
         //   to avoid an overflow of the long data type during primality testing.
         long n = 2 * 2 * 17 * 19 * 48661 * 55441L;

         Assert.Throws<ApplicationException>(() => PrimeFactorization.Get(n));
      }

      public static TheoryData<long, long> SumOfFactorsTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            rv.Add(18, 10);
            rv.Add(6045, 1800);
            rv.Add(56, 28);
            rv.Add(500, 499);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SumOfFactorsTestData))]
      public void SumOfFactors(long expected, long n)
      {
         PrimeFactorization tst = PrimeFactorization.Get(n);

         long actual = tst.SumOfFactors;

         Assert.Equal(expected, actual);
      }
   }
}
