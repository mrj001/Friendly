using System;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class Relations
   {
      private readonly List<Relation> _relations;
      private int _fullyFactoredRelations;
      private int _removedRelations;
      private int _totalRelationsFound;
      private readonly List<PartialRelation> _partialRelations;
      private readonly int _factorBaseSize;

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      public Relations(int factorBaseSize)
      {
         _relations = new();
         _fullyFactoredRelations = 0;
         _removedRelations = 0;
         _totalRelationsFound = 0;
         _partialRelations = new();
         _factorBaseSize = factorBaseSize;
      }

      /// <summary>
      /// Adds a Relation
      /// </summary>
      /// <param name="newRelation">The new Relation to add.</param>
      /// <remarks>
      /// <para>
      /// Adding a Relation via this method increments the FullyFactoredRelations
      /// property.
      /// </para>
      /// </remarks>
      public void AddRelation(Relation newRelation)
      {
         _relations.Add(newRelation);
         _fullyFactoredRelations++;
         _totalRelationsFound++;
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
         _removedRelations++;
      }

      public int RelationCount { get => _relations.Count; }

      /// <summary>
      /// Gets the total number of fully factored Relation objects that were
      /// added.
      /// </summary>
      public int FullyFactoredRelations { get => _fullyFactoredRelations; }

      /// <summary>
      /// Gets the total number of Relation objects that were found.
      /// </summary>
      /// <remarks>
      /// <para>
      /// This includes both fully factored relations, relations from Large Primes
      /// and remove relations.
      /// </para>
      /// </remarks>
      public int TotalRelationsFound { get => _totalRelationsFound; }

      /// <summary>
      /// Gets the count of Removed Relation Objects.
      /// </summary>
      /// <remarks>
      /// <para>
      /// It is not tracked whether the removed Relation Objects originated
      /// from Fully Factored values or from Large Primes.
      /// </para>
      /// </remarks>
      public int RemovedRelations { get => _removedRelations; }

      public void AddPartialRelation(PartialRelation newPartialRelation)
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
         _totalRelationsFound++;
      }

      public int PartialRelationCount { get => _partialRelations.Count; }

      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         IMatrix rv = matrixFactory.GetMatrix(_factorBaseSize, _relations.Count);

         for (int col = 0; col < _relations.Count; col ++)
         {
            Relation rel = _relations[col];
            for (int row = 0; row < _factorBaseSize; row ++)
               if (rel.ExponentVector[row])
                  rv[row, col] = true;
         }

         return rv;
      }

      /// <summary>
      /// Gets an array indicating how many relations were obtained by the number
      /// of large primes involved.
      /// </summary>
      /// <returns></returns>
      /// <remarks>
      /// <para>
      /// The zero'th element indicates that zero large primes were involved.
      /// These values were fully factored during the sieve.  Subsequent indices
      /// correspond to the maximum number of large primes in the constituent
      /// Partial Relations (and Partial Partial and Triples).
      /// </para>
      /// </remarks>
      public int[] GetStats()
      {
         int[] counts = new int[4];
         foreach (Relation j in _relations)
            counts[(int)j.Origin]++;
         return counts;
      }
   }
}

