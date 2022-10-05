using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Friendly.Library.Pollard;

namespace Friendly.Library
{
   public class PrimeFactorization : IList<IPrimeFactor>
   {
      private static long _highestTrialDivisor = 397;

      private readonly List<IPrimeFactor> _factors;

      private int _rhoInvocations;

      public PrimeFactorization(List<IPrimeFactor> factors)
      {
#if DEBUG
         for (int j = 0; j < factors.Count - 2; j++)
            if (factors[j + 1].Factor <= factors[j].Factor)
               throw new ArgumentException("The prime factors must be unique and in increasing order.");
#endif
         _factors = factors;
         _rhoInvocations = 0;
      }

      /// <summary>
      /// Gets or sets the largest prime number that will be used for trial
      /// division.
      /// </summary>
      /// <remarks>If the value given is composite, it will be reduced to the
      /// next largest prime number.</remarks>
      public static long HighestTrialDivisor
      {
         get => _highestTrialDivisor;
         set
         {
            long t = value;
            while (!Primes.IsPrime(t))
               t--;
            _highestTrialDivisor = t;
         }
      }

      /// <summary>
      /// Gets the Prime Factorization of the given number.
      /// </summary>
      /// <param name="n">The number for which to get the Prime Factorizaation</param>
      /// <returns>The Prime Factorization of n.</returns>
      public static PrimeFactorization Get(long n)
      {
         IEnumerator<long> primes = Primes.GetEnumerator();
         long nCopy = n;
         long prime = 0;
         long lastPrimeSquared = 0;
         int exponent;
         long quotient, remainder;
         List<IPrimeFactor> factors = new List<IPrimeFactor>();
         int rhoInvocations = 0;

         while (primes.MoveNext() && nCopy != 1 && lastPrimeSquared < nCopy && prime <= _highestTrialDivisor)
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
            {
               factors.Add(new PrimeFactor(nCopy, 1));
            }
            else
            {
               (List<BigInteger> bigFactors, int tmp) = Factor(prime, nCopy);
               rhoInvocations = tmp;
               bigFactors.Sort();
               int j = 0;
               int pow;
               while (j < bigFactors.Count)
               {
                  pow = 1;
                  while (j + pow < bigFactors.Count && bigFactors[j] == bigFactors[j + pow])
                     pow++;
                  factors.Add(new PrimeFactor(bigFactors[j], pow));
                  j += pow;
               }
            }
         }

         PrimeFactorization rv = new PrimeFactorization(factors);
         rv.RhoInvocations = rhoInvocations;
         Assertions.True(rv.Number == n);
         return rv;
      }

      /// <summary>
      /// Factors the given number, n.
      /// </summary>
      /// <param name="highestPrime">The highest prime used in trial division.</param>
      /// <param name="n">The number to factor.  This must be composite and
      /// not have any prime factors <= highestPrime.</param>
      /// <returns>The first item is a List of factors of n.  Factors may be repeated.  The second
      /// item specifies the number of times the Pollard Rho algorithm was invoked.</returns>
      private static (List<BigInteger>, int) Factor(long highestPrime, BigInteger n)
      {
         List<BigInteger> rv = new();
         BigInteger f1, f2;
         int rhoInvocations = 0;

         // Factor n into two factors
         if (BigInteger.Log10(n) < 18) // TODO: optimize threshold
         {
            PollardRho rho = new PollardRho();
            (f1, f2) = rho.Factor(n);
            rhoInvocations++;
         }
         else
         {
            BigInteger factor;
            int exponent;
            if (IsAPower(highestPrime, n, out factor, out exponent))
            {
               if (Primes.IsPrime(factor))
               {
                  for (int j = 0; j < exponent; j++)
                     rv.Add(factor);
               }
               else
               {
                  (List<BigInteger> factors, int tmp) = Factor(highestPrime, n);
                  rhoInvocations += tmp;
                  for (int j = 0; j < exponent; j++)
                     rv.AddRange(factors);
               }

               return (rv, rhoInvocations);
            }

            QuadraticSieve.QuadraticSieve sieve = new QuadraticSieve.QuadraticSieve(n);
            (f1, f2) = sieve.Factor();
         }

         // Check if each factor is prime; if not factor it too.
         if (Primes.IsPrime(f1))
            rv.Add(f1);
         else
         {
            (List<BigInteger> factors, int k) = Factor(highestPrime, f1);
            rv.AddRange(factors);
            rhoInvocations += k;
         }

         if (Primes.IsPrime(f2))
            rv.Add(f2);
         else
         {
            (List<BigInteger> factors, int k) = Factor(highestPrime, f2);
            rv.AddRange(factors);
            rhoInvocations += k;
         }

         return (rv, rhoInvocations);
      }


      /// <summary>
      /// Determines whether or not the given number, n, is a power of another number.
      /// </summary>
      /// <param name="highestPrime">The highest prime number which has been excluded
      /// from being a factor of n.</param>
      /// <param name="n">The number to check for being a power.</param>
      /// <param name="factor">If n is a power, this returns the base of that power.</param>
      /// <param name="exponent">If n is a power, this returns the exponent.</param>
      /// <returns>True if the number, n, is a power.</returns>
      private static bool IsAPower(long highestPrime, BigInteger n, out BigInteger factor, out int exponent)
      {
         factor = 0;
         exponent = 0;
         IEnumerator<long> j = Primes.GetEnumerator();
         BigInteger root = long.MaxValue;
         while (j.MoveNext() && root > highestPrime)
         {
            root = BigIntegerCalculator.Root(n, (int)j.Current);
            BigInteger val = BigIntegerCalculator.Pow(root, (int)j.Current);
            if (val == n)
            {
               factor = root;
               exponent = (int)j.Current;
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Gets the number of times the Pollard Rho algorithm was invoked during
      /// the factorization of the given number.
      /// </summary>
      public int RhoInvocations
      {
         get => _rhoInvocations;
         private set
         {
            _rhoInvocations = value;
         }
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

      public BigInteger Number
      {
         get
         {
            BigInteger rv = 1;
            foreach (IPrimeFactor f in _factors)
               rv *= BigIntegerCalculator.Pow(f.Factor, f.Exponent);

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
