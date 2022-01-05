using System;
using System.Collections.Generic;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;
using Xunit;

//====================================================================
// References
//====================================================================
//
// A. Kefa Rabah , 2006. Review of Methods for Integer Factorization
//    Applied to Cryptography. Journal of Applied Sciences, 6: 458-481.
//    https://scialert.net/fulltext/?doi=jas.2006.458.481


namespace Test.Library.QuadraticSieve
{
   public class TestQuadraticSieve
   {
      public TestQuadraticSieve()
      {
         TestPrimes.EnsureInitialized();
      }

      public static TheoryData<long[], long> FactorBaseTestData
      {
         get
         {
            var rv = new TheoryData<long[], long>();

            // This is taken from the Quadratic Sieve example in Ref. A.
            rv.Add(new long[] { 2,3,7,11,17,19,23,37,41}, 45_313);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(FactorBaseTestData))]
      public void FactorBase(long[] expected, long n)
      {
         List<long> actual = Friendly.Library.QuadraticSieve.QuadraticSieve.FactorBase(n);

         Assert.Equal(expected.Length, actual.Count);
         for (int j = 0; j < actual.Count; j++)
            Assert.Equal(expected[j], actual[j]);
      }

      [Fact]
      public void FindBSmooth()
      {
         List<long> factorBase = new List<long>{ 2, 3, 7, 11, 17, 19, 23, 37, 41 };
         long n = 45_313;
         List<long> expectedBSmooth = new List<long> { 56, 483, 912, 1776, 3087,
            3528, 3971, 4416, 6216, 7128, 8976, 11808, 12768, 18696, 19712,
            20736, 22287, 27048, 31416, 34776, 35343, 39368 };
         List<int> expectedExpVectors = new List<int>
         {
            0b0000000101,   //    56 == 2**3 * 7**1
            0b0001000110,   //   483 == 3 * 7 * 23
            0b0000100010,   //   912 == 2**4 * 3 * 19
            0b0010000010,   //  1776 == 2**4 * 3 * 37
            0b0000000100,   //  3087 == 3**2 * 7**3
            0b0000000001,   //  3528 == 2**3 * 3**2 * 7**2
            0b0000001000,   //  3971 == 11 * 19**2
            0b0001000010,   //  4416 == 2**6 * 3 * 23
            0b0010000111,   //  6216 == 2**3 * 3 * 7 * 37
            0b0000001001,   //  7128 == 2**3 * 3**4 * 11
            0b0000011010,   //  8976 == 2**4 * 3 * 11 * 17
            0b0100000001,   // 11808 == 2**5 * 3**2 * 41
            0b0000100111,   // 12768 == 2**5 * 3 * 7 * 19
            0b0100100011,   // 18696 == 2**3 * 3 * 19 * 41
            0b0000001100,   // 19712 == 2**8 * 7 * 11
            0b0000000000,   // 20736 == 2**8 * 3**4
            0b0001110010,   // 22287 == 3 * 17 * 19 * 23
            0b0001000011,   // 27048 == 2**3 * 3 * 7**2 * 23
            0b0000011111,   // 31416 == 2**3 * 3 * 7 * 11 * 17
            0b0001000111,   // 34776 == 2**3 * 3**3 * 7 * 23
            0b0000011110,   // 35343 == 3**3 * 7 * 11 * 17
            0b0010100101    // 39368 == 2**3 * 7 * 19 * 37
         };

         (List<long> actualBSmooth, List<BigBitArray> actualExpVectors) = Friendly.Library.QuadraticSieve.QuadraticSieve.FindBSmooth(factorBase, n);

         // Both returned lists are the same length
         Assert.Equal(actualBSmooth.Count, actualExpVectors.Count);

         Assert.Equal(expectedBSmooth.Count, actualBSmooth.Count);
         Assert.Equal(expectedExpVectors.Count, actualExpVectors.Count);

         for (int j = 0; j < expectedBSmooth.Count; j ++)
         {
            Assert.Equal(expectedBSmooth[j], actualBSmooth[j]);
            for (int k = 0, kbit = 1; k < 32; k ++, kbit <<= 1)
               Assert.Equal((expectedExpVectors[j] & kbit) != 0, actualExpVectors[j][k]);
         }
      }
   }
}
