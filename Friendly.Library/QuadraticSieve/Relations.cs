using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// An implementation of the IRelations interface to be used with the Single
   /// Large Prime variation.
   /// </summary>
   public class Relations : IRelations
   {
      private readonly List<Relation> _relations;

      private readonly List<PartialRelation> _partialRelations;
      private readonly int _factorBaseSize;
      private readonly int _maxFactor;
      private readonly long _maxLargePrime;

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The value of the largest prime in the Factor Base.</param>
      /// <param name="maxLargePrime">The maximum value of a residual that will be
      /// considered for the Single Large Prime.</param>
      public Relations(int factorBaseSize, int maxFactor, long maxLargePrime)
      {
         _relations = new();
         _partialRelations = new();
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;
         _maxLargePrime = maxLargePrime;
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // nothing to do here.
      }

      /// <inheritdoc />
      public void Serialize(XmlWriter writer, string name)
      {
         // The Single Large Prime variation is only used when the numbers
         // being factored are small enough we don't need to save progress.
         throw new NotImplementedException();
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         // nothing to do here.
      }

      /// <inheritdoc />
      public void TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         BigInteger residual)
      {
         if (residual == BigInteger.One)
            _relations.Add(new Relation(QofX, x, exponentVector));
         else if(residual < _maxLargePrime && residual > _maxFactor)
            AddPartialRelation(new PartialRelation(QofX, x, exponentVector, (long)residual));
      }

      /// <summary>
      /// Gets the Relation object at the given index.
      /// </summary>
      /// <param name="index">The index of the Relation to return.</param>
      /// <returns>The specified Relation object.</returns>
      public Relation this[int index]
      {
         get
         {
            return _relations[index];
         }
      }

      /// <summary>
      /// Removes the Relation at the given index.
      /// </summary>
      /// <param name="index">The index from which the Relation is removed.</param>
      /// <remarks>
      /// <para>
      /// Use this to remove linearly dependent Relation objects when a
      /// Matrix reduction fails to factor the number.
      /// </para>
      /// </remarks>
      public void RemoveRelationAt(int index)
      {
         _relations.RemoveAt(index);
      }

      /// <inheritdoc />
      public void PrepareToResieve()
      {
         // Nothing to do here.
      }

      /// <inheritdoc />
      public int Count { get => _relations.Count; }

      private void AddPartialRelation(PartialRelation newPartialRelation)
      {
         long p = newPartialRelation.LargePrime;

         int index = 0;
         while (index < _partialRelations.Count && p > _partialRelations[index].LargePrime)
            index++;

         if (index == _partialRelations.Count)
         {
            _partialRelations.Add(newPartialRelation);
            return;
         }

         if (p < _partialRelations[index].LargePrime)
         {
            _partialRelations.Insert(index, newPartialRelation);
            return;
         }

         // We know that:
         //   p == _partialRelations[index].LargePrime
         PartialRelation prev = _partialRelations[index];
         // If we do not remove the Partial Relation, when we get several
         // later ones with the same Large Prime, we will introduce linear
         // dependencies.
         _partialRelations.RemoveAt(index);

         _relations.Add(new Relation(prev, newPartialRelation));
      }

      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         IMatrix rv = matrixFactory.GetMatrix(_factorBaseSize, _relations.Count);

         for (int col = 0; col < _relations.Count; col++)
         {
            Relation rel = _relations[col];
            for (int row = 0; row < _factorBaseSize; row++)
               if (rel.ExponentVector[row])
                  rv[row, col] = true;
         }

         return rv;
      }

      /// <inheritdoc />
      public Statistic[] GetStats()
      {
         int[] counts = new int[4];
         foreach (Relation j in _relations)
            counts[(int)j.Origin]++;

         Statistic[] rv = new Statistic[2];
         rv[0] = new Statistic(StatisticNames.FullyFactored, counts[0]);
         rv[1] = new Statistic(StatisticNames.OneLargePrime, counts[1]);
         return rv;
      }
   }
}

