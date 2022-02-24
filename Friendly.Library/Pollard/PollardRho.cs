using System;
using System.Numerics;

//====================================================================
// References
//====================================================================
//
// A. Pollard's Rho Algorithm, Wikipedia
//    https://en.wikipedia.org/wiki/Pollard%27s_rho_algorithm
//    Accessed: 2022-02-24
//

namespace Friendly.Library.Pollard
{
   public class PollardRho
   {

      private int c = 1;

      public PollardRho()
      {

      }

      public (BigInteger, BigInteger) Factor(BigInteger n)
      {
         BigInteger d;

         c = 1;

         do
         {
            BigInteger x = 2;
            BigInteger y = 2;

            do
            {
               x = f(x, n);
               y = f(f(y, n), n);
               d = BigIntegerCalculator.GCD(x > y ? x - y : y - x, n);
               // Note if x == y, the GCD will be n instead of 1.
            } while (d == 1);

            c++;
         } while (d == n);

         return (d, n / d);
      }

      private BigInteger f(BigInteger x, BigInteger n)
      {
         return (x * x + c) % n;
      }
   }
}