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
