using System;
using System.Numerics;
using System.Reflection;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;
using Xunit;

namespace Test.Library.QuadraticSieve
{
   public class TestPolynomial
   {
      public TestPolynomial()
      {
      }

      private static Polynomial InvokeConstructor(long a, long b, long c, long inv2d)
      {
         ConstructorInfo? info = typeof(Polynomial).GetConstructor(
                  BindingFlags.NonPublic | BindingFlags.Instance, null,
                  new Type[] { typeof(BigInteger), typeof(BigInteger), typeof(BigInteger), typeof(BigInteger) }, null);
         if (info is null)
            throw new ApplicationException("Failed to obtain ConstructorInfo object");

         return (Polynomial)info.Invoke(new object[] { (BigInteger)a, (BigInteger)b, (BigInteger)c, (BigInteger)inv2d });
      }

      public static TheoryData<long, long, long, long> ctorTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long>();

            rv.Add(1, 0, 42, 1);
            rv.Add(1, 2, 3, 1);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctorTestData))]
      public void ctor(long a, long b, long c, long inv2d)
      {
         Polynomial actual = InvokeConstructor(a, b, c, inv2d);

         Assert.Equal(a, actual.A);
         Assert.Equal(b, actual.B);
      }

      public static TheoryData<long, long, long, long, long, long> EvaluateTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, long, long>();

            rv.Add(102, 12, 1, 0, -42, 1);
            rv.Add(7207, 42, 4, 2, 67, 1);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(EvaluateTestData))]
      public void Evaluate(long expected, long x, long a, long b, long c, long inv2d)
      {
         Polynomial poly = InvokeConstructor(a, b, c, inv2d);

         BigInteger actual = poly.Evaluate(x);

         Assert.Equal(expected, actual);
      }
   }
}
