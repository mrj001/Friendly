using System;
using System.Collections;
using System.Collections.Generic;

namespace Friendly.Library
{
   public class PrimeFactorization : IList<PrimeFactor>
   {
      private readonly List<PrimeFactor> _factors;

      private PrimeFactorization(List<PrimeFactor> factors)
      {
         _factors = factors;
      }

      /// <summary>
      /// Gets the Prime
      /// </summary>
      /// <param name="n"></param>
      /// <returns></returns>
      public static PrimeFactorization Get(long n)
      {
         IEnumerator<long> primes = Primes.GetEnumerator();
         long nCopy = n;
         long prime;
         int exponent;
         long quotient, remainder;
         List<PrimeFactor> factors = new List<PrimeFactor>();

         while (primes.MoveNext() && nCopy != 1)
         {
            prime = primes.Current;
            exponent = 0;
            quotient = Math.DivRem(nCopy, prime, out remainder);
            while (remainder == 0)
            {
               nCopy = quotient;
               exponent++;
               quotient = Math.DivRem(nCopy, prime, out remainder);
            }

            if (exponent > 0)
               factors.Add(new PrimeFactor(prime, exponent));
         }

         if (nCopy > 1)
         {
            if (Primes.IsPrime(nCopy))
               factors.Add(new PrimeFactor(nCopy, 1));
            else
               // TODO attempt to factor remaining number
               throw new ApplicationException($"Failed to find prime factorization of {n}; remaining composite: {nCopy}");
         }

         return new PrimeFactorization(factors);
      }

      public PrimeFactor this[int index]
      {
         get => _factors[index];
         set => throw new NotSupportedException();
      }

      public int Count { get => _factors.Count; }

      public bool IsReadOnly { get => true; }

      public void Add(PrimeFactor item)
      {
         throw new NotSupportedException();
      }

      public void Clear()
      {
         throw new NotSupportedException();
      }

      public bool Contains(PrimeFactor item)
      {
         // TODO
         throw new NotImplementedException();
      }

      public void CopyTo(PrimeFactor[] array, int arrayIndex)
      {
         _factors.CopyTo(array, arrayIndex);
      }

      public IEnumerator<PrimeFactor> GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      public int IndexOf(PrimeFactor item)
      {
         // TODO
         throw new NotImplementedException();
      }

      public void Insert(int index, PrimeFactor item)
      {
         throw new NotSupportedException();
      }

      public bool Remove(PrimeFactor item)
      {
         throw new NotSupportedException();
      }

      public void RemoveAt(int index)
      {
         throw new NotSupportedException();
      }
   }
}
