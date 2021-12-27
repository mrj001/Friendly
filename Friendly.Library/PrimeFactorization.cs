using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library
{
   public class PrimeFactorization : IList<IPrimeFactor>
   {
      private readonly List<IPrimeFactor> _factors;

      public PrimeFactorization(List<IPrimeFactor> factors)
      {
#if DEBUG
         for (int j = 0; j < factors.Count - 2; j++)
            if (factors[j + 1].Factor <= factors[j].Factor)
               throw new ArgumentException("The prime factors must be unique and in increasing order.");
#endif
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
         long prime, lastPrimeSquared = 0;
         int exponent;
         long quotient, remainder;
         List<IPrimeFactor> factors = new List<IPrimeFactor>();

         while (primes.MoveNext() && nCopy != 1 && lastPrimeSquared < nCopy)
         {
            prime = primes.Current;
            lastPrimeSquared = prime * prime;
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

      public long SumOfFactors
      {
         get
         {
            long rv = 1;

            foreach (IPrimeFactor f in _factors)
               rv *= (long)((BigInteger.Pow(f.Factor, f.Exponent + 1) - 1) / (f.Factor - 1));

            return rv;
         }
      }

      #region IList<IPrimeFactor>
      public IPrimeFactor this[int index]
      {
         get => _factors[index];
         set => throw new NotSupportedException();
      }

      public int Count { get => _factors.Count; }

      public bool IsReadOnly { get => true; }

      public void Add(IPrimeFactor item)
      {
         throw new NotSupportedException();
      }

      public void Clear()
      {
         throw new NotSupportedException();
      }

      public bool Contains(IPrimeFactor item)
      {
         // TODO
         throw new NotImplementedException();
      }

      public void CopyTo(IPrimeFactor[] array, int arrayIndex)
      {
         _factors.CopyTo(array, arrayIndex);
      }

      public IEnumerator<IPrimeFactor> GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _factors.GetEnumerator();
      }

      public int IndexOf(IPrimeFactor item)
      {
         // TODO
         throw new NotImplementedException();
      }

      public void Insert(int index, IPrimeFactor item)
      {
         throw new NotSupportedException();
      }

      public bool Remove(IPrimeFactor item)
      {
         throw new NotSupportedException();
      }

      public void RemoveAt(int index)
      {
         throw new NotSupportedException();
      }
      #endregion
   }
}
