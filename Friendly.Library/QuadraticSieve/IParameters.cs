using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// An instance of this interface is used to determine various parameters
   /// of the Quadratic Sieve algorithm.
   /// </summary>
   public interface IParameters
   {
      /// <summary>
      /// Gets the number of primes to include in the Factor Base.
      /// </summary>
      /// <param name="n">The number being factored.</param>
      /// <returns>The number of primes to include in the Factor Base.</returns>
      int FindSizeOfFactorBase(BigInteger n);

      /// <summary>
      /// Gets the value of M, the Sieve Interval.
      /// </summary>
      /// <param name="n">The number being factored.</param>
      /// <returns>The size of the Sieve Interval</returns>
      int FindSieveInterval(BigInteger n);

      /// <summary>
      /// Finds the value of T, the large prime tolerance in the
      /// Silverman paper.
      /// </summary>
      /// <param name="n">The number being factored.</param>
      /// <returns>The value of the Large Prime Tolerance.</returns>
      double FindLargePrimeTolerance(BigInteger n);

      /// <summary>
      /// Finds the limit of the small primes to exclude from sieving.
      /// </summary>
      /// <param name="n">The number being factored.</param>
      /// <returns>Factor Base Primes equal to or less than this value are not sieved.</returns>
      int FindSmallPrimeLimit(BigInteger n);

      /// <summary>
      /// Gets the Relations Factory to use to create the Relations object.
      /// </summary>
      /// <returns></returns>
      IRelationsFactory GetRelationsFactory();

      /// <summary>
      /// Gets the maximum number of threads to use for Sieving.
      /// </summary>
      /// <returns></returns>
      int MaxDegreeOfParallelism();
   }
}

