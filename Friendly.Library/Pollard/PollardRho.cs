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
// B. Richard P. Brent, An Improved Monte Carlo Factorization Algorithm,
//    BIT 20 (1980), pp 176 - 184.
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
         long x0 = 2;
         long r = 1;
         long k;
         long m = 100;
         BigInteger x, y, ys, q, G;

         y = x0;
         c = 1;

         do
         {
            do
            {
               x = y;
               for (int i = 0; i < r; i++)
                  y = f(y, n);
               k = 0;
               do
               {
                  ys = y;
                  q = BigInteger.One;
                  for (long i = 0, iul = Math.Min(m, r - k); i < iul; i++)
                  {
                     y = f(y, n);
                     q *= (x > y ? x - y : y - x);
                     q %= n;
                  }
                  G = BigInteger.GreatestCommonDivisor(q, n);
                  k += m;
               } while (k < r && G == BigInteger.One);
               r *= 2;
            } while (G == BigInteger.One);

            if (G == n)
            {
               do
               {
                  ys = f(ys, n);
                  G = BigInteger.GreatestCommonDivisor(x > ys ? x - ys : ys - x, n);
               } while (G == BigInteger.One);
            }

            c++;
         } while (G == n && c < 100);
         if (G == n)
            throw new ApplicationException($"Failed to factor: {n}");

         return (G, n / G);
      }

      private BigInteger f(BigInteger x, BigInteger n)
      {
         return (x * x + c) % n;
      }
   }
}