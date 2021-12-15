﻿using System;
using System.Collections;

namespace Friendly.Library
{
   public static class Primes
   {
      private static BigBitArray _primes;

      /// <summary>
      /// The non-inclusive limit of the Sieved Primes.
      /// </summary>
      private static long _upperSieveLimit = 0;

      /// <summary>
      /// Initializes the Prime checking by running the internal Sieve of Eratosthenes.
      /// </summary>
      /// <param name="ul">The highest number to be run in the Sieve.</param>
      /// <remarks>This must be called prior to calling IsPrime.  If ul is not a multiple of 64, it will
      /// be increased to the next multiple of 64.</remarks>
      public static void Init(long ul)
      {
         _primes = SieveOfEratosthenes(ul);
         _upperSieveLimit = _primes.Capacity;
      }

      /// <summary>
      /// Gets the non-inclusive upper limit of the Sieved Primes.
      /// </summary>
      public static long SieveLimit
      {
         get
         {
            return _upperSieveLimit;
         }
      }

      /// <summary>
      /// Determines if the given value is prime, but only if it is below the SieveLimit.
      /// </summary>
      /// <param name="n">The value to check for primality.</param>
      /// <returns>True if the number if prime; false if the number is composite.</returns>
      /// <remarks>If n >= SieveLimit, an ArgumentException is thrown.</remarks>
      public static bool IsPrimeFast(long n)
      {
         if (n >= _upperSieveLimit)
            throw new ArgumentException($"{nameof(n)} must be less than SieveLimit ({_upperSieveLimit})");
         return !_primes[n];
      }

      /// <summary>
      /// Determines if the given value is prime.
      /// </summary>
      /// <param name="n">The value to check for primality.</param>
      /// <returns>True if the number if prime; false if the number is composite.</returns>
      public static bool IsPrime(long n)
      {
#if DEBUG
         if (n < 0)
            throw new ArgumentOutOfRangeException($"{nameof(n)} must not be negative.");
#endif

         if (n < _upperSieveLimit)
            return !_primes[n];
         else
            throw new ArgumentOutOfRangeException($"{nameof(n)} exceeds the upper limit of {_upperSieveLimit}");
      }

      /// <summary>
      /// Returns a bit vector wherein each set bit corresponds to a composite number. 
      /// </summary>
      /// <param name="ul">The highest number (inclusive) to be sieved.</param>
      /// <returns>a bit vector wherein each set bit corresponds to a composite number. </returns>
      /// <remarks>False / clear bits indicate a prime number.</remarks>
      private static BigBitArray SieveOfEratosthenes(long ul)
      {
         BigBitArray rv = new BigBitArray(ul + 1);

         // Seed this with 0 and 1 as composite.
         rv[0] = true;
         rv[1] = true;

         long index;
         long j = 2 * 2;

         // Mark all even numbers as composite.
         do
         {
            rv[j] = true;
            j += 2;
         } while (j < ul);

         // Mark all odd numbers as composite.
         j = 3;
         while (j * j < ul)
         {
            index = 2 * j;
            while (index <= ul)
            {
               rv[index] = true;
               index += j;
            }

            // Advance to next prime
            do
            {
               j += 2;
            } while (j * j < ul && rv[j]);
         }

         return rv;
      }
   }
}
