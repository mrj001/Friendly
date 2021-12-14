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
   }
}
