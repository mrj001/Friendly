using System;
using System.Collections.Generic;
using Friendly.Library;
using Moq;
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

            // Only one prime factor remains after trial division
            rv.Add(new (long, int)[] { (7, 1) , (11, 1), (13, 1), (60149, 1) }, 7 * 11 * 13 * 60149);

            // Two prime factors remain after trial division.
            rv.Add(new (long, int)[] { (2, 2), (17, 1), (19, 1), (48661, 1), (55441, 1) },
               2L * 2 * 17 * 19 * 48661 * 55441);

            // 3 large prime factors remain after trial division.
            rv.Add(new (long, int)[] { (2, 1), (3, 2), (5, 1), (33119, 1), (33149, 1), (33151, 1) },
               2L * 3 * 3 * 5 * 33119 * 33149 * 33151);

            // A cube remains after trial division
            rv.Add(new (long, int)[] { (11, 2), (33151, 3) }, 11L * 11 * 33151 * 33151 * 33151);

            // A sufficiently large cube remains after trial division to invoke the
            // quadratic sieve branch.  Due to its being a power, the
            // Quadratic Sieve is not actually invoked.
            rv.Add(new (long, int)[] { (7, 1), (11, 1), (107_309, 3) }, 7L * 11 * 107_309 * 107_309 * 107_309);

            // A sufficiently large value remains after trial division to
            // invoke the Quadratic Sieve.
            rv.Add(new (long, int)[] { (2, 1), (3, 1), (1_066_246_453, 1), (1_092_547_727, 1) }, 2L * 3 * 1_066_246_453 * 1_092_547_727);
            

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

      public static TheoryData<long> NumberTestData
      {
         get
         {
            var rv = new TheoryData<long>();

            rv.Add(28);
            rv.Add(48);
            rv.Add(49);
            rv.Add(149 * 149 * 163 * 163);

            // Only one prime factor remains after trial division
            rv.Add(7 * 11 * 13 * 60149);

            // Two prime factors remain after trial division.
            rv.Add(2L * 2 * 17 * 19 * 48661 * 55441);

            // 3 large prime factors remain after trial division.
            rv.Add(2L * 3 * 3 * 5 * 33119 * 33149 * 33151);

            // A cube remains after trial division
            rv.Add(11L * 11 * 33151 * 33151 * 33151);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(NumberTestData))]
      public void Number(long n)
      {
         PrimeFactorization actual = PrimeFactorization.Get(n);

         Assert.Equal(n, actual.Number);
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

      [Fact]
      public void SumOfFactors_Large()
      {
         long f1 = 5;
         long f2 = 3_037_000_573;
         long expected = 1 + f1 + f2 + f1 * f2;

         List<IPrimeFactor> factors = new List<IPrimeFactor>(2);

         Mock<IPrimeFactor> mock = new Mock<IPrimeFactor>(MockBehavior.Strict);
         mock.Setup(m => m.Factor).Returns(f1);
         mock.Setup(m => m.Exponent).Returns(1);
         factors.Add(mock.Object);

         mock = new Mock<IPrimeFactor>(MockBehavior.Strict);
         mock.Setup(m => m.Factor).Returns(f2);
         mock.Setup(m => m.Exponent).Returns(1);
         factors.Add(mock.Object);

         PrimeFactorization factorization = new PrimeFactorization(factors);

         long actual = factorization.SumOfFactors;

         Assert.Equal(expected, actual);
      }
   }
}
