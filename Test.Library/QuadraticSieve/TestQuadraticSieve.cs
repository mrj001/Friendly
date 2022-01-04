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
   }
}
