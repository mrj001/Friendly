using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
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
      // TODO: Should be mocked.
      /// <summary>
      /// A fake Relations Factory that ONLY uses the Single Large Prime Variaion.
      /// </summary>
      private class FakeRelationsFactory : IRelationsFactory
      {
         public IRelations GetRelations(int numDigits, int factorBaseSize, int maxFactor, long maxLargePrime)
         {
            return new Relations(factorBaseSize, maxFactor, maxLargePrime);
         }

         public IRelations GetRelations(int factorBaseSize, int maxFactor, XmlNode relationsNode)
         {
            throw new NotImplementedException();
         }
      }

      /// <summary>
      /// A fake Relations Factory that ONLY uses the Double Large Prime Variation.
      /// </summary>
      private class FakeRelationsFactory2P : IRelationsFactory
      {
         public IRelations GetRelations(int numDigits, int factorBaseSize, int maxFactor, long maxLargePrime)
         {
            return new Relations2P(factorBaseSize, maxFactor, maxLargePrime);
         }

         public IRelations GetRelations(int factorBaseSize, int maxFactor, XmlNode relationsNode)
         {
            throw new NotImplementedException();
         }
      }

      /// <summary>
      /// A Fake RelationsFactory that only uses the Triple Large Prime Variation.
      /// </summary>
      private class FakeRelationsFactory3P : IRelationsFactory
      {
         public IRelations GetRelations(int numDigits, int factorBaseSize, int maxFactor, long maxLargePrime)
         {
            return new Relations3P(factorBaseSize, maxFactor, maxLargePrime);
         }

         public IRelations GetRelations(int factorBaseSize, int maxFactor, XmlNode relationsNode)
         {
            throw new NotImplementedException();
         }
      }

      private enum LargePrimeType
      {
         OneLargePrime,
         TwoLargePrimes,
         ThreeLargePrimes
      }

      private class FakeParameters : IParameters
      {
         private readonly IParameters _parameters;
         private readonly IRelationsFactory _relationsFactory;
         private readonly bool _maxDopSpecified;
         private readonly int _maxDegreeOfParallelism;

         public FakeParameters(LargePrimeType largePrimeType)
         {
            _parameters = new Parameters();
            _maxDopSpecified = false;
            if (largePrimeType == LargePrimeType.OneLargePrime)
               _relationsFactory = new FakeRelationsFactory();
            else if (largePrimeType == LargePrimeType.TwoLargePrimes)
               _relationsFactory = new FakeRelationsFactory2P();
            else if (largePrimeType == LargePrimeType.ThreeLargePrimes)
               _relationsFactory = new FakeRelationsFactory3P();
            else
               throw new ArgumentException();
         }

         public FakeParameters(LargePrimeType largePrimeType,  int maxDegreeOfParallelism)
         {
            _parameters = new Parameters();
            _maxDopSpecified = true;
            _maxDegreeOfParallelism = MaxDegreeOfParallelism();
            if (largePrimeType == LargePrimeType.OneLargePrime)
               _relationsFactory = new FakeRelationsFactory();
            else if (largePrimeType == LargePrimeType.TwoLargePrimes)
               _relationsFactory = new FakeRelationsFactory2P();
            else if (largePrimeType == LargePrimeType.ThreeLargePrimes)
               _relationsFactory = new FakeRelationsFactory3P();
            else
               throw new ArgumentException();
         }

         public double FindLargePrimeTolerance(BigInteger n)
         {
            return _parameters.FindLargePrimeTolerance(n);
         }

         public int FindSieveInterval(BigInteger n)
         {
            return _parameters.FindSieveInterval(n);
         }

         public int FindSizeOfFactorBase(BigInteger n)
         {
            return _parameters.FindSizeOfFactorBase(n);
         }

         public int FindSmallPrimeLimit(BigInteger n)
         {
            return _parameters.FindSmallPrimeLimit(n);
         }

         public IRelationsFactory GetRelationsFactory()
         {
            return _relationsFactory;
         }

         public int MaxDegreeOfParallelism()
         {
            if (_maxDopSpecified)
               return _maxDegreeOfParallelism;
            else
               return _parameters.MaxDegreeOfParallelism();
         }
      }

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

      private void InternalFactor(IParameters parameters, long f1, long f2)
      {
         if (f1 > f2)
         {
            long t = f1;
            f1 = f2;
            f2 = t;
         }

         long product = f1 * f2;

         Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(parameters, product);
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

      /// <summary>
      /// Test Factoring of specified primes using the Single Large Prime Variation.
      /// </summary>
      /// <param name="f1"></param>
      /// <param name="f2"></param>
      [Theory]
      [MemberData(nameof(FactorTestData))]
      public void Factor(long f1, long f2)
      {
         IParameters parameters = new FakeParameters(LargePrimeType.OneLargePrime);
         InternalFactor(parameters, f1, f2);
      }

      /// <summary>
      /// Test Factoring of specified primes using the Double Large Prime Variation.
      /// </summary>
      /// <param name="f1"></param>
      /// <param name="f2"></param>
      [Theory]
      [MemberData(nameof(FactorTestData))]
      public void Factor2P(long f1, long f2)
      {
         IParameters parameters = new FakeParameters(LargePrimeType.TwoLargePrimes);
         InternalFactor(parameters, f1, f2);
      }

      private void InternalFactor2(IParameters parameters)
      {
         IEnumerator<long> j = Primes.GetEnumerator();
         List<long> primes = new List<long>();

         while (j.MoveNext() && j.Current < Primes.SieveLimit / 2) ;

         while (j.MoveNext() && j.Current < Primes.SieveLimit)
            primes.Add(j.Current);
         Assert.True(primes.Count > 2);

         int p1Index, p2Index;
         Random rnd = new Random(123);

         for (int k = 0; k < 100; k++)
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

            Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(parameters, n);
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

      // Multiply a bunch of pseudo-randomly chosen pairs of primes together and
      // factor the resulting number to make sure it works.
      // Uses the Single Large Prime variation.
      [Fact]
      public void Factor2()
      {
         IParameters parameters = new FakeParameters(LargePrimeType.OneLargePrime);
         InternalFactor2(parameters);
      }

      // Multiply a bunch of pseudo-randomly chosen pairs of primes together and
      // factor the resulting number to make sure it works.
      // Uses the Double Large Prime variation.
      [Fact]
      public void Factor2_2P()
      {
         IParameters parameters = new FakeParameters(LargePrimeType.TwoLargePrimes);
         InternalFactor2(parameters);
      }

      #region factoring with 3 large primes
      public static TheoryData<BigInteger, BigInteger> Factor3P_ResieveTestData
      {
         get
         {
            var rv = new TheoryData<BigInteger, BigInteger>();

            rv.Add(BigInteger.Parse("413348176517"), BigInteger.Parse("744178154147"));

            return rv;
         }
      }

      [MemberData(nameof(Factor3P_ResieveTestData))]
      [Theory]
      public void Factor3P_Resieve(BigInteger f1, BigInteger f2)
      {
         //Primes.Init(2_147_483_648);
         BigInteger n = f1 * f2;
         if (f1 > f2)
         {
            BigInteger t = f1;
            f1 = f2;
            f2 = t;
         }

         IParameters parameters = new FakeParameters(LargePrimeType.ThreeLargePrimes, 1);
         Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(parameters, n);
         (BigInteger g1, BigInteger g2) = sieve.Factor();

         if (g1 > g2)
         {
            BigInteger tmp = g1;
            g1 = g2;
            g2 = tmp;
         }

         Assert.Equal(f1, g1);
         Assert.Equal(f2, g2);
      }
      #endregion

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

      private void InternalFactorBig(IParameters parameters, BigInteger f1, BigInteger f2)
      {
         if (f1 > f2)
         {
            BigInteger t = f1;
            f1 = f2;
            f2 = t;
         }

         BigInteger product = f1 * f2;

         Friendly.Library.QuadraticSieve.QuadraticSieve sieve = new Friendly.Library.QuadraticSieve.QuadraticSieve(parameters, product);
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

      [Theory(Skip ="Very slow running.")]
      [MemberData(nameof(FactorBigTestData))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
      public void FactorBig1P(int serial, BigInteger f1, BigInteger f2)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
      {
         IParameters parameters = new FakeParameters(LargePrimeType.OneLargePrime);
         InternalFactorBig(parameters, f1, f2);
      }

      [Theory(Skip = "Very slow running.")]
      [MemberData(nameof(FactorBigTestData))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
      public void FactorBig2P(int serial, BigInteger f1, BigInteger f2)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
      {
         IParameters parameters = new FakeParameters(LargePrimeType.TwoLargePrimes);
         InternalFactorBig(parameters, f1, f2);
      }
   }
}
