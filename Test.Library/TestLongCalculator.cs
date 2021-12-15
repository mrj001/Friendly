using System;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestLongCalculator
   {
      public static TheoryData<long, long, long> GCDTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long>();

            rv.Add(1, 7523, 1783);
            rv.Add(47, 47 * 93923, 47 * 77849);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(GCDTestData))]
      public void GCD(long expectedGcd, long a, long b)
      {
         long actualGcd = LongCalculator.GCD(a, b);

         Assert.Equal(expectedGcd, actualGcd);
      }

      [Fact]
      public void GCD_Throws()
      {
#if DEBUG
         Assert.Throws<ArgumentException>(() => LongCalculator.GCD(512, 513));
#endif
      }

      public static TheoryData<long, long,long, long> ModPowTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long>();

            rv.Add(1, 42, 0, 4999);
            rv.Add(61511, 61511, 1, 72739);

            rv.Add(25, 5, 2, 72739);
            rv.Add(2, 5, 2, 23);

            rv.Add(125, 5, 3, 49999);
            rv.Add(10, 5, 3, 23);

            rv.Add(625, 5, 4, 49999);
            rv.Add(4, 5, 4, 23);

            // result per Wolfram Alpha for 4999^65537 mod 1000000007
            rv.Add(972_518_196, 4999, 65537, 1_000_000_007);
            // 4999^65536 mod1000000007
            rv.Add(180_030_511, 4999, 65536, 1_000_000_007);
            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ModPowTestData))]
      public void ModPow(long expected, long val, long exponent, long modulus)
      {
         long actual = LongCalculator.ModPow(val, exponent, modulus);

         Assert.Equal(expected, actual);
      }
   }
}
