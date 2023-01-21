﻿using System.Xml;

namespace Friendly.Library.QuadraticSieve
{
   public interface IRelationsFactory
   {
      /// <summary>
      /// Gets the IRelations instance to be used to track the Relations.
      /// </summary>
      /// <param name="largePrimeStrategy">Specifies the number of Large Primes to
      /// use in building Relations.</param>
      /// <param name="numDigits">The number of digits in the number being factored.</param>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The largest prime in the Factor Base.</param>
      /// <returns>An instance of IRelations.</returns>
      IRelations GetRelations(LargePrimeStrategy largePrimeStrategy, int numDigits,
         int factorBaseSize, int maxFactor);

      /// <summary>
      /// Gets an IRelations instance when restarting from saved state.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The largest prime in the Factor Base.</param>
      /// <param name="rdr"></param>
      /// <returns>An instance of IRelations.</returns>
      IRelations GetRelations(int factorBaseSize, int maxFactor, XmlReader rdr);
   }
}