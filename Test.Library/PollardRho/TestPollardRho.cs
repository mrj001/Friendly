using System;
using System.Numerics;
using Friendly.Library.Pollard;
using Xunit;

namespace Test.Library.Pollard
{
   public class TestPollardRho
   {
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
            rv.Add(70_051, 292_183);
            rv.Add(76_487, 89_209);
            rv.Add(39_667, 73_721);
            rv.Add(41_893, 43_403);
            rv.Add(44_927, 111_337);
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

         BigInteger n = f1 * f2;
         PollardRho rho = new PollardRho();

         (BigInteger actual1, BigInteger actual2) = rho.Factor(n);

         if (actual1 > actual2)
         {
            BigInteger t = actual1;
            actual1 = actual2;
            actual2 = t;
         }

         Assert.Equal(f1, actual1);
         Assert.Equal(f2, actual2);
      }

      public static TheoryData<long> Factor2TestData
      {
         get
         {
            var rv = new TheoryData<long>();

            // This number failed to factor during a Benchmark run.
            rv.Add(21_116_263_079);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(Factor2TestData))]
      public void Factor2(long n)
      {
         PollardRho rho = new PollardRho();

         (BigInteger f1, BigInteger f2) = rho.Factor(n);

         Assert.Equal(n, f1 * f2);
      }
   }
}
