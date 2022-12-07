using System;
using System.Xml;

namespace Friendly.Library.QuadraticSieve
{
   public class RelationsFactory : IRelationsFactory
   {
      public RelationsFactory()
      {
      }

      /// <inheritdoc />
      public IRelations GetRelations(int numDigits, int factorBaseSize,
         int maxFactor, long maxLargePrime)
      {
         return new Relations3P(factorBaseSize, maxFactor, maxLargePrime);
         //if (numDigits > 59)
         //   return new Relations2P(factorBaseSize, maxFactor, maxLargePrime);
         //else
         //   return new Relations(factorBaseSize, maxFactor, maxLargePrime);
      }

      /// <inheritdoc />
      public IRelations GetRelations(XmlNode relationsNode)
      {
         // TODO: Determine which IRelations implementation to create
         // TODO: validate that we aren't attempting to create one that supports too few large primes.
         return new Relations3P(relationsNode);
      }
   }
}

