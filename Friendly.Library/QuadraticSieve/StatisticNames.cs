using System;
namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   ///  A static class for defining the names of statistics related to the
   ///  Quadratic Sieve.
   /// </summary>
   public static class StatisticNames
   {
      /// <summary>
      /// This name indicates the number of Relation objects where the value
      /// was fully factored over the Factor Base.
      /// </summary>
      public const string FullyFactored = "FullyFactored";
      /// <summary>
      /// This name indicates the number of Relation objects that were formed
      /// from two Large Prime Relations.
      /// </summary>
      public const string OneLargePrime = "OneLargePrime";

      /// <summary>
      /// This name indicates the number of Relation objects that were formed
      /// from at least one Relation with Two Large Primes (but none with
      /// Three Large Primes).
      /// </summary>
      public const string TwoLargePrimes = "TwoLargePrimes";

      /// <summary>
      /// This name indicates the number of Relation objects that were formed
      /// from at least one Relation with Three Large Primes.
      /// </summary>
      public const string ThreeLargePrimes = "ThreeLargePrimes";

      /// <summary>
      /// Tracks the time spent factoring.
      /// </summary>
      public const string FactoringTime = "FactoringTime";

      /// <summary>
      /// The total number of Polynomials used in the factorization.
      /// </summary>
      public const string TotalPolynomials = "TotalPolynomials";
   }
}

