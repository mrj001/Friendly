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
         XmlNode relationsNode)
      {
         XmlNode typeNode = relationsNode.FirstChild!;
         SerializeHelper.ValidateNode(typeNode, Relations2P.TypeNodeName);
         string type = typeNode.InnerText;

         switch(type)
         {
            case "Relations2P":
               return new Relations2P(factorBaseSize, maxFactor, relationsNode);

            case "Relations3P":
               return new Relations3P(factorBaseSize, maxFactor, relationsNode);

            default:
               throw new ArgumentException($"Unrecognized Relations type: '{type}'");
         }
      }
   }
}

