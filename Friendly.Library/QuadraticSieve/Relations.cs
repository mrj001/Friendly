using System;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class Relations
   {
      private readonly BigInteger _kn;
      private readonly List<Relation> _relations;
      private int _fullyFactoredRelations;
      private int _removedRelations;
      private int _totalRelationsFound;
      private readonly List<PartialRelation> _partialRelations;
      private readonly int _factorBaseSize;

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="kn">The integer being factored, with the pre-multiplier applied.</param>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      public Relations(BigInteger kn, int factorBaseSize)
      {
         _kn = kn;
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
         // p == _partialRelations[index]
         PartialRelation prev = _partialRelations[index];
         _partialRelations.RemoveAt(index);   // TODO: do we have to remove it?
                                              // TODO: can we extract a relationship from every pair with the same large prime?
         //BigInteger invLargePrime = BigIntegerCalculator.FindInverse(p, _kn);

         BigInteger q = newPartialRelation.QOfX * prev.QOfX;
         BigInteger x = newPartialRelation.X * prev.X;
         BigBitArray exponentVector = new BigBitArray(newPartialRelation.ExponentVector);
         exponentVector.Xor(prev.ExponentVector);

         _relations.Add(new Relation(q, x, exponentVector));
         _totalRelationsFound++;
      }

      public int PartialRelationCount { get => _partialRelations.Count; }

      public Matrix GetMatrix()
      {
         Matrix rv = new(_factorBaseSize, _relations.Count, 0);

         for (int col = 0; col < _relations.Count; col ++)
         {
            Relation rel = _relations[col];
            for (int row = 0; row < _factorBaseSize; row ++)
               if (rel.ExponentVector[row])
                  rv[row, col] = true;
         }

         return rv;
      }
   }
}

