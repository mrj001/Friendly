using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class FactorBase : IList<FactorBasePrime>
   {
      private readonly List<FactorBasePrime> _factors;

      private readonly int _multiplier;
      private readonly BigInteger _kn;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="multiplier">The small multiplier being considered for
      /// use with this Factor Base.</param>
      /// <param name="kn">The number being factored (premultiplied by k).</param>
      /// <param name="size">The number of Primes to include in the Factor Base.</param>
      public FactorBase(int multiplier, BigInteger kn, int size)
      {
         _multiplier = multiplier;
         _kn = kn;
         _factors = new(size);

         // Always add -1
         _factors.Add(new FactorBasePrime(-1));

         // We can always include 2 because of the condition that
         // n == 1 mod 8
         _factors.Add(new FactorBasePrime(2));

         // Add primes p such that (n % p) is a quadratic residue modulo p.
         // Note that for primes in the second argument, the Jacobi Symbol
         // reduces to the Legendre Symbol.
         IEnumerator<long> primes = Primes.GetEnumerator();
         primes.MoveNext();  // Skip 2
         while (primes.MoveNext() && _factors.Count < size)
         {
            long prime = primes.Current;
            if (1 == LongCalculator.JacobiSymbol((long)(_kn % prime), prime))
               _factors.Add(new FactorBasePrime((int)prime));
         }
      }

      /// <summary>
      /// Evaluates the Knuth-Schroeppel function for this Factor Base.
      /// </summary>
      /// <returns>The value of the Knuth-Schroeppel function.</returns>
      public double KnuthSchroeppel()
      {
         double rv = 0;

         foreach (FactorBasePrime j in _factors)
         {
            long p = j.Prime;
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

      /// <summary>
      /// Gets the value of the largest Prime in the Factor Base.
      /// </summary>
      public long MaxPrime { get => _factors[_factors.Count - 1].Prime; }

      /// <summary>
      /// Called on the chosen Factor Base to calculate the Square Root of
      /// kN modulo each prime in the Factor Base.
      /// </summary>
      public void CalculateSquareRoots()
      {
         foreach(FactorBasePrime prime in _factors)
         {
            if (prime.Prime < 0 || prime.Prime == 2) continue;
            prime.InitRootN((int)BigIntegerCalculator.SquareRoot(_kn, prime.Prime));
         }
      }

      public FactorBasePrime this[int index]
      {
         get => _factors[index];
         set => throw new NotSupportedException();
      }

      public int Count { get => _factors.Count; }

      public bool IsReadOnly { get => true; }

      public void Add(FactorBasePrime item)
      {
         throw new NotSupportedException();
      }

      public void Clear()
      {
         throw new NotSupportedException();
      }

      public bool Contains(FactorBasePrime item)
      {
         return _factors.Contains(item);
      }

      public void CopyTo(FactorBasePrime[] array, int arrayIndex)
      {
         _factors.CopyTo(array, arrayIndex);
      }

      public IEnumerator<FactorBasePrime> GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      public int IndexOf(FactorBasePrime item)
      {
         return _factors.IndexOf(item);
      }

      public void Insert(int index, FactorBasePrime item)
      {
         throw new NotSupportedException();
      }

      public bool Remove(FactorBasePrime item)
      {
         throw new NotSupportedException();
      }

      public void RemoveAt(int index)
      {
         throw new NotSupportedException();
      }
   }
}

