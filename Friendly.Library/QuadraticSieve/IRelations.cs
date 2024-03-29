﻿using System.Numerics;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   public interface IRelations : ISerialize
   {
      Relation this[int index] { get; }

      /// <summary>
      /// Gets the number of Relations we can use in constructing the columns of
      /// the Matrix.
      /// </summary>
      int Count { get; }

      /// <summary>
      /// Tries to add a Relation
      /// </summary>
      /// <param name="QofX"></param>
      /// <param name="x"></param>
      /// <param name="exponentVector"></param>
      /// <param name="residual"></param>
      /// <remarks>
      /// <para>
      /// A Relation will not be added if the residual fails to meet the conditions
      /// necessary for the class implementing IRelations.
      /// </para>
      /// </remarks>
      void TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         BigInteger residual);

      void RemoveRelationAt(int index);

      /// <summary>
      /// Performs any actions needed to accept additional sieving after matrix
      /// reduction failed to find a null vector that factorized the number.
      /// </summary>
      void PrepareToResieve();

      /// <summary>
      /// Gets an array of Statistic objects to provide information about the
      /// factoring.
      /// </summary>
      /// <returns></returns>
      Statistic[] GetStats();

      IMatrix GetMatrix(IMatrixFactory matrixFactory);
   }
}