using System;
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

      private static Polynomial InvokeConstructor(long a, long b, long c)
      {
         ConstructorInfo? info = typeof(Polynomial).GetConstructor(
                  BindingFlags.NonPublic | BindingFlags.Instance, null,
                  new Type[] { typeof(long), typeof(long), typeof(long) }, null);
         if (info is null)
            throw new ApplicationException("Failed to obtain ConstructorInfo object");

         return (Polynomial)info.Invoke(new object[] { a, b, c });
      }

      public static TheoryData<long, long, long> ctorTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long>();

            rv.Add(1, 0, 42);
            rv.Add(1, 2, 3);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctorTestData))]
      public void ctor(long a, long b, long c)
      {
         Polynomial actual = InvokeConstructor(a, b, c);

         Assert.Equal(a, actual.A);
         Assert.Equal(b, actual.B);
         Assert.Equal(c, actual.C);
      }

      public static TheoryData<long, long, long, long, long> EvaluateTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, long>();

            rv.Add(102, 12, 1, 0, -42);
            rv.Add(7291, 42, 4, 2, 67);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(EvaluateTestData))]
      public void Evaluate(long expected, long x, long a, long b, long c)
      {
         Polynomial poly = InvokeConstructor(a, b, c);

         long actual = poly.Evaluate(x);

         Assert.Equal(expected, actual);
      }
   }
}
