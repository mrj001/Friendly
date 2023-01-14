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

