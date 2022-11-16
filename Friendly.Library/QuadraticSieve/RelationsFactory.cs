using System;
namespace Friendly.Library.QuadraticSieve
{
   public class RelationsFactory : IRelationsFactory
   {
      public RelationsFactory()
      {
      }

      /// <inheritdoc />
      public IRelations GetRelations(int numDigits, int factorBaseSize,
         int maxFactor, int maxLargePrime)
      {
         return new Relations(factorBaseSize, maxFactor, maxLargePrime);
      }
   }
}

