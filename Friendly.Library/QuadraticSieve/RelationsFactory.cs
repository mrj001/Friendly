#nullable enable
using System;
using System.Xml;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   public class RelationsFactory : IRelationsFactory
   {
      public RelationsFactory()
      {
      }

      /// <inheritdoc />
      public IRelations GetRelations(LargePrimeStrategy largePrimeStrategy,
         int numDigits, int factorBaseSize, int maxFactor)
      {
         switch(largePrimeStrategy)
         {
            case LargePrimeStrategy.OneLargePrime:
               return new Relations(factorBaseSize, maxFactor);

            case LargePrimeStrategy.TwoLargePrimes:
               return new Relations2P(factorBaseSize, maxFactor);

            case LargePrimeStrategy.ThreeLargePrimes:
               return new Relations3P(factorBaseSize, maxFactor);

            default:
               throw new ArgumentException($"Unrecognized value for LargePrimeStrategy: {largePrimeStrategy}");
         }
      }

      /// <inheritdoc />
      public IRelations GetRelations(int factorBaseSize, int maxFactor,
         XmlReader rdr)
      {
         rdr.ReadStartElement();
         rdr.ReadStartElement(Relations2P.TypeNodeName);
         string type = rdr.ReadContentAsString();
         rdr.ReadEndElement();

         switch(type)
         {
            case "Relations2P":
               return new Relations2P(factorBaseSize, maxFactor, rdr);

            case "Relations3P":
               return new Relations3P(factorBaseSize, maxFactor, rdr);

            default:
               throw new ArgumentException($"Unrecognized Relations type: '{type}'");
         }
      }
   }
}

