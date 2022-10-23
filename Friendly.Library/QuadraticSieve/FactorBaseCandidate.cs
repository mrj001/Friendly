using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class FactorBaseCandidate
   {
      private readonly List<int> _factors;
      private readonly int _multiplier;
      private readonly BigInteger _kn;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="multiplier">The small multiplier being considered for
      /// use with this Factor Base.</param>
      /// <param name="kn">The number being factored (premultiplied by k).</param>
      /// <param name="size">The number of Primes to include in the Factor Base.</param>
      private FactorBaseCandidate(int multiplier, BigInteger kn, int size)
      {
         _multiplier = multiplier;
         _kn = kn;
         _factors = new(size);

         // Always add -1
         _factors.Add(-1);

         // We can always include 2 because of the condition that
         // n == 1 mod 8
         _factors.Add(2);

         // Add primes p such that (n % p) is a quadratic residue modulo p.
         // Note that for primes in the second argument, the Jacobi Symbol
         // reduces to the Legendre Symbol.
         IEnumerator<long> primes = Primes.GetEnumerator();
         primes.MoveNext();  // Skip 2
         while (primes.MoveNext() && _factors.Count < size)
         {
            long prime = primes.Current;
            if (1 == LongCalculator.JacobiSymbol((long)(_kn % prime), prime))
               _factors.Add((int)prime);
         }
      }

      public static FactorBase GetFactorBase(BigInteger nOrig)
      {
         BigInteger n;
         int[] nSmallMultipliersToConsider = new int[] { 1, 3, 5, 7, 11, 13, 17,
            19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97 };
         FactorBaseCandidate[] factorBases = new FactorBaseCandidate[nSmallMultipliersToConsider.Length];
         int indexOfMax = int.MinValue;
         double maxKnuthSchroeppel = double.MinValue;

         for (int j = 0; j < nSmallMultipliersToConsider.Length; j++)
         {
            n = nOrig * nSmallMultipliersToConsider[j];
            if ((n & 7) == 1)  // n == 1 mod 8.
            {
               int sz = FindSizeOfFactorBase(n);
               factorBases[j] = new FactorBaseCandidate(nSmallMultipliersToConsider[j], n, sz);

               // Evaluate the Knuth-Schroeppel function and find its maximum value
               // over the potential Factor Bases.
               double knuthSchroeppel = factorBases[j].KnuthSchroeppel();
               if (knuthSchroeppel > maxKnuthSchroeppel)
               {
                  indexOfMax = j;
                  maxKnuthSchroeppel = knuthSchroeppel;
               }
            }
         }

         // TODO Does there exists a possibility that no small multiplier satisfied
         //  the condition _nOrig * m mod 8 == 1?  In this event, this will
         //  throw an exception.
         return new FactorBase(nSmallMultipliersToConsider[indexOfMax],
            nOrig * nSmallMultipliersToConsider[indexOfMax],
            factorBases[indexOfMax]._factors.Count, factorBases[indexOfMax]._factors);
      }

      private static int FindSizeOfFactorBase(BigInteger kn)
      {
         // Table 1 of Ref B is unclear as to the meaning of "Factor Base Size".
         // Is this the number of primes in the Factor Base?  Or is it the
         // maximum size of the primes to consider for membership in the
         // Factor Base?
         // Here, the first option is chosen.
         // Additionally, for numbers smaller than 24 digits, the size has
         // been extrapolated based upon halving the size of the factor base
         // for each 6 digit reduction in size.
         int[] digits = new int[] { 12, 18, 24, 30, 36, 42, 48, 54, 60, 66 };
         int[] sz = new int[] { 25, 50, 100, 200, 400, 900, 1200, 2000, 3000, 4500 };

         int numDigits = 1 + (int)Math.Floor(BigInteger.Log10(kn));
         int j = 0;
         while (j < digits.Length && digits[j] < numDigits)
            j++;

         if (j == digits.Length)
            return sz[sz.Length - 1];

         return sz[j];
      }

      /// <summary>
      /// Evaluates the Knuth-Schroeppel function for this Factor Base.
      /// </summary>
      /// <returns>The value of the Knuth-Schroeppel function.</returns>
      private double KnuthSchroeppel()
      {
         double rv = 0;

         foreach (int j in _factors)
         {
            long p = j;
            if (p < 0) continue;

            double logp = Math.Log(p);
            double g;
            if (p == 2)
            {
               g = (_kn & 7) == 1 ? 2 : 0;
            }
            else
            {
               if (_multiplier % p != 0)
                  g = 2.0 / p;
               else
                  g = 1.0 / p;
            }

            rv += logp * g;
         }

         return rv - 0.5 * Math.Log(_multiplier);
      }
   }
}

