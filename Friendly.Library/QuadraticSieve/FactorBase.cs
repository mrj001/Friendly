using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class FactorBase : IList<FactorBasePrime>
   {
      private readonly FactorBasePrime[] _factors;

      private readonly int _multiplier;
      private readonly BigInteger _kn;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="multiplier">The small multiplier being considered for
      /// use with this Factor Base.</param>
      /// <param name="kn">The number being factored (premultiplied by k).</param>
      /// <param name="size">The number of Primes in the Factor Base.</param>
      /// <param name="primes">The Primes to include in the Factor Base.</param>
      public FactorBase(int multiplier, BigInteger kn, int size, IList<int> primes)
      {
         _multiplier = multiplier;
         _kn = kn;
         _factors = new FactorBasePrime[size];

         // Special case for -1
         int index = 0;
         IEnumerator<int> primeEnumerator = primes.GetEnumerator();
         primeEnumerator.MoveNext();
         _factors[index] = new FactorBasePrime(primeEnumerator.Current, 0);
         index++;

         // Special case for 2
         primeEnumerator.MoveNext();
         _factors[index] = new FactorBasePrime(primeEnumerator.Current, 0);
         index++;

         while (primeEnumerator.MoveNext())
         {
            int prime = primeEnumerator.Current;
            int root = (int)BigIntegerCalculator.SquareRoot(_kn, prime);
            _factors[index] = new FactorBasePrime(prime, root);
            index++;
         }
      }

      /// <summary>
      /// Gets the value of the largest Prime in the Factor Base.
      /// </summary>
      public long MaxPrime { get => _factors[_factors.Length - 1].Prime; }

      /// <summary>
      /// Gets the small pre-multiplier value used.
      /// </summary>
      public int Multiplier { get => _multiplier; }

      public FactorBasePrime this[int index]
      {
         get => _factors[index];
         set => throw new NotSupportedException();
      }

      public int Count { get => _factors.Length; }

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
         throw new NotImplementedException();
      }

      public void CopyTo(FactorBasePrime[] array, int arrayIndex)
      {
         _factors.CopyTo(array, arrayIndex);
      }

      public IEnumerator<FactorBasePrime> GetEnumerator()
      {
         return (IEnumerator<FactorBasePrime>)_factors.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      public int IndexOf(FactorBasePrime item)
      {
         throw new NotImplementedException();
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

