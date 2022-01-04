using System;
using System.Collections.Generic;
using Friendly.Library;

//====================================================================
// References
//====================================================================
//
// A. Kefa Rabah , 2006. Review of Methods for Integer Factorization
//    Applied to Cryptography. Journal of Applied Sciences, 6: 458-481.
//    https://scialert.net/fulltext/?doi=jas.2006.458.481

namespace Friendly.Library.QuadraticSieve
{
   public static class QuadraticSieve
   {
      /// <summary>
      /// Determines an appropriate factor base for factoring the given number
      /// </summary>
      /// <param name="n">The number being factored.</param>
      /// <returns>A factor base to use for factoring the given number.</returns>
      internal static List<long> FactorBase(long n)
      {
         double logn = Math.Log(n);
         // Number of odd primes to consider for the Factor Base per Ref. A.
         int sz = (int)Math.Floor(Math.Sqrt(Math.Exp(Math.Sqrt(logn * Math.Log(logn)))));
         List<long> rv = new List<long>(sz);

         // We can always include 2 because any odd number squared will have 1 as a
         // Quadratic Residue modulo 2.
         rv.Add(2);

         // Add primes p such that (n % p) is a quadratic residue modulo p.
         // Note that for primes in the second argument, the Jacobi Symbol
         // reduces to the Legendre Symbol.
         IEnumerator<long> j = Primes.GetEnumerator();
         j.MoveNext();  // Skip 2
         int k = 0;
         while (j.MoveNext() && k < sz)
         {
            if (1 == LongCalculator.JacobiSymbol(n % j.Current, j.Current))
               rv.Add(j.Current);
            k++;
         }

         return rv;
      }
   }
}
