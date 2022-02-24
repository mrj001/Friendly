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
