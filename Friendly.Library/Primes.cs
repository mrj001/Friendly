using System;
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

      // References:
      //  https://en.wikipedia.org/wiki/Miller–Rabin_primality_test
      /// <summary>
      /// The set of bases to use when doing an iterated Miller-Rabin Test.
      /// </summary>
      /// <remarks>This is enough to reliably test all numbers up to 2**64.</remarks>
      private static long[] _millerRabinBases = new long[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

      // Reference:
      // https://crypto.stanford.edu/pbc/notes/numbertheory/millerrabin.html
      /// <summary>
      /// Performs an iterated Miller-Rabin test with several bases.
      /// </summary>
      /// <param name="n">An odd number to be tested for primality.</param>
      /// <returns>True if the number is probably prime; false if definitely composite.</returns>
      public static bool MillerRabin(long n)
      {
#if DEBUG
         if ((n & 1) == 0)
            throw new ArgumentException($"{nameof(n)} must be odd");
#endif
         foreach (long a in _millerRabinBases)
            if (!MillerRabin(n, a))
               return false;

         return true;
      }

      /// <summary>
      /// Performs a Miller-Rabin test with the given base.
      /// </summary>
      /// <param name="n">An odd number to be tested for primality.</param>
      /// <param name="a">The base to be used during the primality test.</param>
      /// <returns>True if the number is probably prime; false if definitely composite.</returns>
      private static bool MillerRabin(long n, long a)
      {
         long exp = n - 1;
         long s = 0;
         while ((exp & 1) == 0)
         {
            exp >>= 1;
            s++;
         }

         int i = 1;
         long[] x = new long[s + 1];
         x[0] = LongCalculator.ModPow(a, exp, n);

         while (exp < n - 1)
         {
            x[i] = x[i-1] * x[i-1] % n;
            exp <<= 1;
            i++;
         }
         i--;

         // This amounts to the Fermat test
         if (x[i] != 1) return false;

         while (i >= 0 && x[i] == 1)
            i--;

         if (i < 0 || x[i] == n - 1) return true;

         return false;
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
