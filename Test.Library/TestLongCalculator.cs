﻿using System;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestLongCalculator
   {

      public TestLongCalculator()
      {
         // Some tests depend upon the Primes sieve having been run.
         // We ensure that it is run first by instantiating an instance of TestPrimes
         TestPrimes throwAway = new TestPrimes();
      }

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

      public static TheoryData<long, long> FindInverseTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            rv.Add(42, 499);
            rv.Add(720, 65_537);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(FindInverseTestData))]
      public void FindInverse(long a, long n)
      {
         long actual = LongCalculator.FindInverse(a, n);

         Assert.Equal(1, (actual * a) % n);
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

      public static TheoryData<long, long, int> PowTestData
      {
         get
         {
            var rv = new TheoryData<long, long, int>();

            rv.Add(8_589_934_592, 2, 33);
            rv.Add(124_925_014_999, 4999, 3);
            rv.Add(1, 499, 0);
            rv.Add(33, 33, 1);
            rv.Add(1089, 33, 2);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(PowTestData))]
      public void Pow(long expected, long val, int exponent)
      {
         long actual = LongCalculator.Pow(val, exponent);

         Assert.Equal(expected, actual);
      }

      public static TheoryData<bool, long, long> IsQuadraticResidueTestData
      {
         get
         {
            var rv = new TheoryData<bool, long, long>();

            // Note: all values for p must be less than TestPrimes._upperLimit;

            // From Wikipedia article on Quadratic residues
            rv.Add(true, 35, 73);
            rv.Add(false, 22, 73);

            // Confirmed via Wolfram-Alpha
            rv.Add(false, 45, 25013);
            rv.Add(true, 47, 25013);   // solutions are 8920 & 16093

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(IsQuadraticResidueTestData))]
      public void IsQuadraticResidue(bool expected, long a, long p)
      {
         TestPrimes.EnsureInitialized();
         bool actual = LongCalculator.IsQuadraticResidue(a, p);

         Assert.Equal(expected, actual);
      }

      public static TheoryData<long, long, long> JacobiSymbolTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long>();

            // These test data were drawn from the examples in the
            // Jacobi Reference cited in the method under test.
            rv.Add(1, 1783, 7523);       // example 1
            rv.Add(1, 756479, 1298351);  // example 2
            rv.Add(-1, 4852777, 12408107);  // example 3
            rv.Add(1, 17327467, 48746413);  // example 4

            // This value was calculated using Wolfram-Alpha.
            rv.Add(1, 35912, 55457);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(JacobiSymbolTestData))]
      public void JacobiSymbol(long expected, long a, long n)
      {
         long actual = LongCalculator.JacobiSymbol(a, n);

         Assert.Equal(expected, actual);
      }

      public static TheoryData<long, long> SquareRootTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            rv.Add(65536, 0);
            rv.Add(766_998_091, 250_000);

            // A 31-bit number squared, plus twice itself.
            // This should make a 62-bit product which is one less than the
            // next highest square number.
            rv.Add(1_296_501_914, 2 * 1_296_501_914L);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SquareRootTestData))]
      public void SquareRoot(long expectedRoot, long offset)
      {
         long value = expectedRoot * expectedRoot + offset;

         long actualRoot = LongCalculator.SquareRoot(value);

         Assert.Equal(expectedRoot, actualRoot);
      }

      public static TheoryData<long, long> SquareRootModuloTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            rv.Add(2, 13);        // p % 4 == 1
            rv.Add(3200, 55457);  // p % 4 == 1
            rv.Add(423,73679);    // p % 4 == 3

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SquareRootModuloTestData))]
      public void SquareRootModulo(long root, long p)
      {
         long n = root * root % p;

         long actual = LongCalculator.SquareRoot(n, p);

         Assert.True(root == actual || p - root == actual);
      }

      [Fact]
      public void SquareRootModulo_Throws()
      {
#if DEBUG
         // Throws an ArgumentException because 5 is not a quadratic residue of 13.
         Assert.Throws<ArgumentException>(() => LongCalculator.SquareRoot(5, 13));

         // Throws an Argument Exception because the modulus is not prime.
         Assert.Throws<ArgumentException>(() => LongCalculator.SquareRoot(42, 17 * 19));
#endif
      }

      public static TheoryData<long, long, int> RootTestData
      {
         get
         {
            var rv = new TheoryData<long, long, int>();

            // Test exact cube root.
            rv.Add(65_537, 65_537L * 65_537 * 65_537, 3);

            // Test exact 7th root.
            rv.Add(43, 43L * 43 * 43 * 43 * 43 * 43 * 43, 7);

            // Test one less than a 4th power.
            rv.Add(11272, 11273L * 11273 * 11273 * 11273 - 1, 4);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(RootTestData))]
      public void Root(long expectedRoot, long n, int y)
      {
         long actualRoot = LongCalculator.Root(n, y);

         Assert.Equal(expectedRoot, actualRoot);
      }
   }
}
