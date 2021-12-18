using System;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestPrimeFactor
   {
      public TestPrimeFactor()
      {
         if (Primes.SieveLimit == 0)
            Primes.Init(32768);
      }

      [Fact]
      public void ctor()
      {
         long prime = 2;
         int exponent = 3;
         PrimeFactor factor = new PrimeFactor(prime, exponent);

         Assert.Equal(prime, factor.Factor);
         Assert.Equal(exponent, factor.Exponent);
      }

      [Fact]
      public void ctor_Throws()
      {
         long notPrime = 65536;
         int exponent = 42;

         Assert.Throws<ArgumentException>(() => new PrimeFactor(notPrime, exponent));
      }
   }
}
