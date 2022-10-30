using System;
using System.Numerics;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestBigIntegerCalculator
   {
      public TestBigIntegerCalculator()
      {
      }

      public static TheoryData<BigInteger, BigInteger, BigInteger> GCDTestData
      {
         get
         {
            var rv = new TheoryData<BigInteger, BigInteger, BigInteger>();

            rv.Add(1, 7523, 1783);
            rv.Add(47, 47 * 93923, 47 * 77849);

            // The following two test cases use prime numbers > 2**32,
            // which were generated using Wolfram-Alpha:
            // "random prime between 4,300,000,000 and 4,400,000,000"
            rv.Add(56, 56 * 4_383_521_357, 56 * 4_339_939_033);
            rv.Add(65_537, (new BigInteger(65_537)) * 4_331_304_973 * 4_335_665_377,
               (new BigInteger(65_537)) * 4_365_330_709 * 4_374_926_749);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(GCDTestData))]
      public void GCD(BigInteger expectedGcd, BigInteger a, BigInteger b)
      {
         BigInteger actualGcd = BigIntegerCalculator.GCD(a, b);

         Assert.Equal(expectedGcd, actualGcd);
      }

      public static TheoryData<int, long> GetNumberOfDigitsTestData
      {
         get
         {
            var rv = new TheoryData<int, long>();

            rv.Add(6, 999_999);
            rv.Add(7, 1_000_000);

            rv.Add(9, 999_999_999);
            rv.Add(10, 1_000_000_000);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(GetNumberOfDigitsTestData))]
      public void GetNumberofDigits(int expectedNumberOfDigits, long n)
      {
         int actual = BigIntegerCalculator.GetNumberOfDigits(n);

         Assert.Equal(expectedNumberOfDigits, actual);
      }

      public static TheoryData<long, long> SquareRootTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            // Test that we get the right answer for a perfect square.
            rv.Add(5_000_000_029, 0);

            // Test that we get the floor (and not the ceiling)
            // for one less than the next square number.
            rv.Add(5_000_000_039, 2 * 5_000_000_039);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SquareRootTestData))]
      public void SquareRoot(long expectedRoot, long offset)
      {
         BigInteger value = (new BigInteger(expectedRoot)) * expectedRoot + offset;

         BigInteger actualRoot = BigIntegerCalculator.SquareRoot(value);

         Assert.Equal(expectedRoot, actualRoot);
      }
   }
}
