using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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
         _factors = GetFactors(_kn, size);
      }

      private static List<int> GetFactors(BigInteger kn, int factorBaseSize)
      {
         List<int> factors = new(factorBaseSize);

         // Always add -1
         factors.Add(-1);

         // We can always include 2 because of the condition that
         // n == 1 mod 8
         factors.Add(2);

         // Add primes p such that (n % p) is a quadratic residue modulo p.
         // Note that for primes in the second argument, the Jacobi Symbol
         // reduces to the Legendre Symbol.
         IEnumerator<long> primes = Primes.GetEnumerator();
         primes.MoveNext();  // Skip 2
         while (primes.MoveNext() && factors.Count < factorBaseSize)
         {
            long prime = primes.Current;
            if (1 == LongCalculator.JacobiSymbol((long)(kn % prime), prime))
               factors.Add((int)prime);
         }

         return factors;
      }

      /// <summary>
      /// Gets a suitable Factor Base for factoring the given integer. 
      /// </summary>
      /// <param name="parameters">An instance of IParameters which supplies
      /// the Quadratic Sieve algorithm parameters.</param>
      /// <param name="nOrig">The original number being factored. </param>
      /// <returns>An instance of a FactorBase.</returns>
      public static FactorBase GetFactorBase(IParameters parameters, BigInteger nOrig)
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
               int sz = parameters.FindSizeOfFactorBase(nOrig);
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

      /// <summary>
      /// Gets a Factor Base when the multiplier and size have previously been determined.
      /// </summary>
      /// <param name="multiplier"></param>
      /// <param name="nOrig"></param>
      /// <param name="factorBaseSize"></param>
      /// <returns></returns>
      public static FactorBase GetFactorBase(int multiplier, BigInteger nOrig, int factorBaseSize)
      {
         BigInteger kn = multiplier * nOrig;
         List<int> factors = GetFactors(kn, factorBaseSize);
         return new FactorBase(multiplier, kn, factorBaseSize, factors);
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

