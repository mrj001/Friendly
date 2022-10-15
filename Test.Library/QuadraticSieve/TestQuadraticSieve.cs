using System;
using System.Collections.Generic;
using System.Numerics;
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

      public static TheoryData<long, long> FactorTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            rv.Add(10_247, 10_267);
            rv.Add(83717, 96097);
            rv.Add(98563, 85661);
            rv.Add(86371, 99391);
            rv.Add(1_092_547_727, 1_066_246_453);

            // 20_467_711_333 is the first number encountered which ran out
            // of squares before successfully factoring.
            // The factors were provided by Wolfram-Alpha.
            rv.Add(70_051, 292_183);

            // 102,349,931,745 caused an integer overflow in the Quadratic Sieve.
            // Wolfram-Alpha provided the factorization.
            // Factors of 3 & 5 not included here.
            rv.Add(76_487, 89_209);

            // The same situation as 102,349,931,745:
            // 102,350,181,745 = 5 * 7 * 39,667 * 73,721
            rv.Add(39_667, 73_721);

            // With the small multiplier to make the number of the form
            // n == 1 mod 4, this factorization causes an integer overflow.
            // 100,005,503,345 = 5 * 11 * 41,893 * 43,403
            rv.Add(41_893, 43_403);

            // 2 * 2 * 5 * 44927 * 111337 caused a negative value to be
            // passed to the IsPrime method, which threw an exception.
            rv.Add(44_927, 111_337);

            // This caused an integer overflow (commit id: 8d8299b)
            // So few B-Smooth numbers are found that Q(x) = X**2 - n
            // overflows when evaluating X**2.
            // 100,123,265,325 == 3 * 5 * 5  * 33619 * 39709
            rv.Add(33_619, 39_709);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(FactorTestData))]
      public void Factor(long f1, long f2)
      {
         if (f1 > f2)
         {
            long t = f1;
            f1 = f2;
            f2 = t;
         }

         long product = f1 * f2;

         Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(product);
         (BigInteger actual1, BigInteger actual2) = sieve.Factor();

         if (actual1 > actual2)
         {
            BigInteger t = actual1;
            actual1 = actual2;
            actual2 = t;
         }

         Assert.Equal(f1, actual1);
         Assert.Equal(f2, actual2);
      }

      // Multiply a bunch of pseudo-randomly chosen pairs of primes together and
      // factor the resulting number to make sure it works.
      [Fact]
      public void Factor2()
      {
         IEnumerator<long> j = Primes.GetEnumerator();
         List<long> primes = new List<long>();

         while (j.MoveNext() && j.Current < Primes.SieveLimit / 2) ;

         while (j.MoveNext() && j.Current < Primes.SieveLimit)
            primes.Add(j.Current);
         Assert.True(primes.Count > 2);

         int p1Index, p2Index;
         Random rnd = new Random(123);

         for (int k = 0; k < 100; k ++)
         {
            p1Index = rnd.Next(primes.Count);
            do
            {
               p2Index = rnd.Next(primes.Count);
            } while (p2Index == p1Index);

            long p1 = primes[p1Index];
            long p2 = primes[p2Index];

            long n = p1 * p2;

            if (p1 > p2)
            {
               long t = p1;
               p1 = p2;
               p2 = t;
            }

            Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(n);
            (BigInteger q1, BigInteger q2) = sieve.Factor();
            if (q1 > q2)
            {
               BigInteger t = q1;
               q1 = q2;
               q2 = t;
            }

            Assert.Equal(p1, q1);
            Assert.Equal(p2, q2);
         }
      }

      public static TheoryData<int, BigInteger, BigInteger> FactorBigTestData
      {
         get
         {
            var rv = new TheoryData<int, BigInteger, BigInteger>();

            rv.Add(0,
               BigInteger.Parse("326971659889765076797128839"),
               BigInteger.Parse("528807794908360128130942333"));

            rv.Add(1,
               BigInteger.Parse("770607400433253661528625143"),
               BigInteger.Parse("915047396402611445606787673"));

            rv.Add(2,
               BigInteger.Parse("326971659889765076797128905916787"),
               BigInteger.Parse("528807794908360128130942957240397"));

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(FactorBigTestData))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
      public void FactorBig(int serial, BigInteger f1, BigInteger f2)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
      {
         if (f1 > f2)
         {
            BigInteger t = f1;
            f1 = f2;
            f2 = t;
         }

         BigInteger product = f1 * f2;

         Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(product);
         (BigInteger actual1, BigInteger actual2) = sieve.Factor();

         if (actual1 > actual2)
         {
            BigInteger t = actual1;
            actual1 = actual2;
            actual2 = t;
         }

         Assert.Equal(f1, actual1);
         Assert.Equal(f2, actual2);
      }
   }
}
