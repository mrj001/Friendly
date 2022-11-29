#nullable enable
using System;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// A simple Name/Value pair for returning statistical information.
   /// </summary>
   public class Statistic
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

      private readonly string _name;
      private readonly object _value;

      public Statistic(string name, object value)
      {
         _name = name;
         _value = value;
      }

      public string Name { get => _name; }

      public object Value { get => _value; }

      public override string ToString()
      {
         return $"{_name}: {_value}";
      }
   }
}

