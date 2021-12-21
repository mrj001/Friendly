using System;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestFraction
   {
      public static TheoryData<long, long, long, long> ctorTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long>();

            rv.Add(1, 3, 1, 3);
            rv.Add(-1, 6, 1, -6);
            rv.Add(2, 5, 4, 10);
            rv.Add(3, 7, 9, 21);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctorTestData))]
      public void ctor(long expectedNumerator, long expectedDenominator, long numerator, long denominator)
      {
         Fraction tst = new Fraction(numerator, denominator);

         Assert.Equal(expectedNumerator, tst.Numerator);
         Assert.Equal(expectedDenominator, tst.Denominator);
      }

      [Fact]
      public void ctor_Throws()
      {
         Assert.Throws<DivideByZeroException>(() => new Fraction(5, 0));
      }

      public static TheoryData<long, long> GetInverseTestData
      {
         get
         {
            var rv = new TheoryData<long, long>();

            rv.Add(3, 4);
            rv.Add(2, 7);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(GetInverseTestData))]
      public void GetInverse(long num, long den)
      {
         Fraction f = new Fraction(num, den);

         Fraction actual = f.GetInverse();

         Assert.Equal(den, actual.Numerator);
         Assert.Equal(num, actual.Denominator);
      }

      [Fact]
      public void GetInverseThrows()
      {
         Fraction f1 = Fraction.Zero;

         Assert.Throws<DivideByZeroException>(() => f1.GetInverse());
      }

      [Fact]
      public void Equality()
      {
         int num = 5;
         int den = 499;

         Fraction f1 = new Fraction(num, den);
         Fraction f2 = new Fraction(num, den);
         Fraction f3 = new Fraction(num + 1, den);

         Assert.False(Object.ReferenceEquals(f1, f2));
         Assert.True(f1.Equals(f2));
         Assert.True(f1.Equals((object)f2));
         Assert.True(f1 == f2);
         Assert.False(f1 != f2);

         Assert.False(f1.Equals(f3));
         Assert.False(f1.Equals((object)f3));
         Assert.False(f1 == f3);
         Assert.True(f1 != f3);
      }

      [Fact]
      public void Comparison()
      {
         int n1 = 5;   // 5/13 == 0.3846...
         int d1 = 13;
         int n2 = 7;   // 7/17 == 0.4118...
         int d2 = 17;

         Fraction f1 = new Fraction(n1, d1);
         Fraction f2 = new Fraction(n2, d2);

         Assert.False(f1 < null);
         Assert.False(f1 <= null);
         Assert.True(f1 > null);
         Assert.True(f1 >= null);

         Assert.True(f1 < f2);
         Assert.True(f1 <= f2);
         Assert.True(f2 > f1);
         Assert.True(f2 >= f1);
      }

      public static TheoryData<long, long, long, long, long, long> AddTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, long, long>();

            rv.Add(2, 5, 1, 5, 1, 5);
            rv.Add(17, 35, 1, 5, 2, 7);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(AddTestData))]
      public void Add(long expectedNum, long expectedDen, long num1, long den1, long num2, long den2)
      {
         Fraction f1 = new Fraction(num1, den1);
         Fraction f2 = new Fraction(num2, den2);

         Fraction actual = f1 + f2;

         Assert.Equal(expectedNum, actual.Numerator);
         Assert.Equal(expectedDen, actual.Denominator);
      }

      public static TheoryData<long, long, long, long, long, long> SubtractTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, long, long>();

            rv.Add(2, 5, 1, 5, -1, 5);
            rv.Add(1, 5, 17, 35, 2, 7);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SubtractTestData))]
      public void Subtract(long expectedNum, long expectedDen, long num1, long den1, long num2, long den2)
      {
         Fraction f1 = new Fraction(num1, den1);
         Fraction f2 = new Fraction(num2, den2);

         Fraction actual = f1 - f2;

         Assert.Equal(expectedNum, actual.Numerator);
         Assert.Equal(expectedDen, actual.Denominator);
      }

      public static TheoryData<long, long, long, long, long, long> MultiplyTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, long, long>();

            rv.Add(2, 7, 2, 5, 5, 7);
            rv.Add(3, 23 * 19, 1, 23, 3, 19);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(MultiplyTestData))]
      public void Multiply(long expectedNum, long expectedDen, long num1, long den1, long num2, long den2)
      {
         Fraction f1 = new Fraction(num1, den1);
         Fraction f2 = new Fraction(num2, den2);

         Fraction actual = f1 * f2;

         Assert.Equal(expectedNum, actual.Numerator);
         Assert.Equal(expectedDen, actual.Denominator);
      }

      public static TheoryData<long, long, long, long, long, long> DivideTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, long, long>();

            rv.Add(1, 5, 1, 1, 5, 1);
            rv.Add(1, 23, 3, 23 * 19, 3, 19);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(DivideTestData))]
      public void Divide(long expectedNum, long expectedDen, long num1, long den1, long num2, long den2)
      {
         Fraction f1 = new Fraction(num1, den1);
         Fraction f2 = new Fraction(num2, den2);

         Fraction actual = f1 / f2;

         Assert.Equal(expectedNum, actual.Numerator);
         Assert.Equal(expectedDen, actual.Denominator);
      }
   }
}
