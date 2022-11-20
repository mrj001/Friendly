#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Friendly.Library.Pollard;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// An implementation of the IRelations interface to be used with the Two
   /// Large Primes variation.
   /// </summary>
   public class Relations2P : IRelations
   {
      #region Member Data
      /// <summary>
      /// The set of fully factored Relations found during sieving.
      /// </summary>
      private readonly List<Relation> _relations;

      private readonly int _factorBaseSize;
      private readonly int _maxFactor;
      private readonly long _maxLargePrime;

      /// <summary>
      /// The maximum value of the residual that will be considered for inclusion
      /// in a Two Large Primes Relation.
      /// </summary>
      private readonly long _maxTwoPrimes;

      /// <summary>
      /// The current count of components within the Graph.
      /// </summary>
      private int _componentCount;

      /// <summary>
      /// This Dictionary maps each prime (the key) to the parent prime within
      /// the Union-Find algorithm.  If the key equals the value, this prime is
      /// the root of its component.
      /// </summary>
      private readonly Dictionary<long, long> _components;

      private readonly List<PartialPartialRelation> _partialPartialRelations;
      #endregion

      /// <summary>
      /// Constructs a collection of Relations.
      /// </summary>
      /// <param name="factorBaseSize">The number of primes in the Factor Base.</param>
      /// <param name="maxFactor">The value of the largest prime in the Factor Base.</param>
      /// <param name="maxLargePrime">The maximum value of a residual that will be
      /// considered for a Relation with a Single Large Prime.</param>
      public Relations2P(int factorBaseSize, int maxFactor, long maxLargePrime)
      {
         _relations = new();
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;
         _maxLargePrime = maxLargePrime;

         _maxTwoPrimes = _maxLargePrime > int.MaxValue ? long.MaxValue : _maxLargePrime * _maxLargePrime;

         _partialPartialRelations = new List<PartialPartialRelation>();

         // Create the Graph
         // We initialize the Graph with the special "prime" of one.
         // This way no special code is needed to add it when adding the first
         // Partial Relation.
         _componentCount = 1;
         _components = new Dictionary<long, long>(1 << 23);
         _components.Add(1, 1);
      }

      /// <summary>
      /// Finds the "root" of the current set (component) of vertices.
      /// </summary>
      /// <param name="prime">The prime to find the root of.</param>
      /// <returns>The prime which is serving as the root of the given prime's component.</returns>
      private long Find(long prime)
      {
         // Is this prime already in the Graph?
         if (!_components.ContainsKey(prime))
         {
            // Add the new component as its own root.
            _componentCount++;
            _components.Add(prime, prime);
            return prime;
         }

         // Find the root of this component, keeping track of intermediate
         // ancestors.
         List<long> ancestors = new();
         long r = prime;
         while (_components[r] != r)
         {
            ancestors.Add(r);
            r = _components[r];
         }

         // Update all intermediate ancestors to point to the root.
         foreach (long p in ancestors)
            _components[p] = r;

         return r;
      }

      /// <summary>
      /// Merges two components into a single component
      /// </summary>
      /// <param name="p1">A prime representing the first component.</param>
      /// <param name="p2">A prime representing the second component.</param>
      private void Union(long p1, long p2)
      {
         long r1 = Find(p1);
         long r2 = Find(p2);

         // Are they already part of the same component?
         if (r1 == r2)
            return;

         // Smaller primes occur more often than larger ones, so join the larger
         // prime to the smaller primes component.  This will usually join the
         // smaller component to the larger.  See Ref C, section 2 (Counting
         // Fundamental Cycles)
         if (p2 < p1)
         {
            long tmp = p2;
            p2 = p1;
            p1 = tmp;
            tmp = r2;
            r2 = r1;
            r1 = tmp;
         }

         // Merge the r2 component into the r1 component.
         // TODO: How expensive is it to iterate over the entire collection?
         foreach (long key in _components.Keys)
            if (_components[key] == r2)
               _components[key] = r1;
      }

      /// <inheritdoc />
      public bool TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         BigInteger residual)
      {
         if (residual == BigInteger.One)
         {
            _relations.Add(new Relation(QofX, x, exponentVector));
            return true;
         }
         else if (residual < _maxLargePrime && residual > _maxFactor)
         {
            AddPartialRelation(QofX, x, exponentVector, 1, (long)residual);
            return true;
         }
         else if (residual < _maxTwoPrimes && !Primes.IsPrime(residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger p1, BigInteger p2) = rho.Factor(residual);
            if (p1 != p2)
               AddPartialRelation(QofX, x, exponentVector, (long)p1, (long)p2);
            else
               _relations.Add(new Relation(QofX, x, exponentVector, RelationOrigin.TwoLargePrimes));
            return true;
         }

         return false;
      }

      private void AddPartialRelation(BigInteger QofX, BigInteger x,
         BigBitArray exponentVector, long p1, long p2)
      {
         if (p1 > p2)
         {
            long tmp = p1;
            p1 = p2;
            p2 = tmp;
         }

         // Adding the edge that this Partial Partial Relation represents
         // corresponds to joining these two components, if they're not already
         // the same.
         Union(p1, p2);

         _partialPartialRelations.Add(new PartialPartialRelation(QofX, x, exponentVector, p1, p2));
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
      public int Count
      {
         // See Ref. C, section 2 (Counting Fundamental Cycles)
         // The number of edges is the number of Partial Partial Relations
         // The number of vertices is the number of primes in _components.
         get => _relations.Count +
            _partialPartialRelations.Count + _componentCount - _components.Count;
      }

      private class Vertex : IEquatable<Vertex>, IEnumerable<PartialPartialRelation>
      {
         private readonly long _prime;
         private readonly Vertex? _ancestor;
         private readonly int _depth;
         private readonly List<PartialPartialRelation> _edges;

         public Vertex(long prime)
         {
            _prime = prime;
            _ancestor = null;
            _depth = 0;
            _edges = new List<PartialPartialRelation>();
         }

         public Vertex(long prime, Vertex ancestor, PartialPartialRelation incomingEdge)
         {
            _prime = prime;
            _ancestor = ancestor;
            _depth = ancestor.Depth + 1;
            _edges = new List<PartialPartialRelation>();
            _edges.Add(incomingEdge);
         }

         public void Add(PartialPartialRelation edge)
         {
            _edges.Add(edge);
         }

         public IEnumerator<PartialPartialRelation> GetEnumerator()
         {
            return _edges.GetEnumerator();
         }

         IEnumerator IEnumerable.GetEnumerator()
         {
            return _edges.GetEnumerator();
         }

         public int Depth { get => _depth; }

         public long Prime { get => _prime; }

         public Vertex? Ancestor { get => _ancestor; }

         public override int GetHashCode()
         {
            return _prime.GetHashCode();
         }

         public override bool Equals(object? obj)
         {
            return Equals(obj as Vertex);
         }

         public bool Equals(Vertex? other)
         {
            if (other is null)
               return false;

            return _prime == other.Prime;
         }
      }

      /// <summary>
      /// Gets the root prime of each component identified by the Component
      /// Counting procedure.
      /// </summary>
      /// <returns>Each Key in the returned Dictionary is a root prime of a Component.
      /// The corresponding Values contain a List<> of the primes (other than the root) in that Component.</returns>
      private Dictionary<long, List<long>> GetComponentRoots()
      {
         Dictionary<long, List<long>> rv = new(_componentCount);

         foreach(KeyValuePair<long, long> kvp in _components)
         {
            List<long> lst;

            if (!rv.ContainsKey(kvp.Value))
            {
               lst = new();
               rv.Add(kvp.Value, lst);
            }
            else
            {
               lst = rv[kvp.Value];
               lst.Add(kvp.Key);
            }
         }

         return rv;
      }

      /// <summary>
      /// Construct a Dictionary for mapping primes (vertices) to the
      /// corresponding set of Partial & Partial Partial Relations (edges).
      /// </summary>
      /// <returns>A mapping from primes to PartialPartialRelation objects involving those primes.</returns>
      private Dictionary<long, List<PartialPartialRelation>> GetRelationsByPrimes()
      {
         Dictionary<long, List<PartialPartialRelation>> rbp = new(_partialPartialRelations.Count);

         foreach(PartialPartialRelation ppr in _partialPartialRelations)
         {
            long p = ppr.Prime1;
            List<PartialPartialRelation> lst;
            if (!rbp.ContainsKey(p))
            {
               lst = new List<PartialPartialRelation>();
               rbp.Add(p, lst);
            }
            else
            {
               lst = rbp[p];
            }
            lst.Add(ppr);

            p = ppr.Prime2;
            if (!rbp.ContainsKey(p))
            {
               lst = new List<PartialPartialRelation>();
               rbp.Add(p, lst);
            }
            else
            {
               lst = rbp[p];
            }
            lst.Add(ppr);
         }

         return rbp;
      }

      /// <summary>
      /// Combines all the Components in the Graph to find cycles and
      /// combine them into new Relation objects.
      /// </summary>
      private void CombineComponents()
      {
         CombineSingleLargePrimes();

         Dictionary<long, List<PartialPartialRelation>> rbp = GetRelationsByPrimes();
         Dictionary<long, List<long>> roots = GetComponentRoots();

         foreach (KeyValuePair<long, List<long>> root in roots)
         {
            // If there is only one prime in the component:
            //   1. it will be the root
            //   2. the List will be empty
            //   3. it cannot be part of a cycle.
            if (root.Value.Count == 0)
               continue;

            CombineComponent(root.Key, root.Value, rbp);
         }
      }

      /// <summary>
      /// Combines pairs of Partial Relations into Relation objects.
      /// </summary>
      private void CombineSingleLargePrimes()
      {
         List<PartialPartialRelation> singles = new(_partialPartialRelations.Where((x) => x.Prime1 == 1 && !x.Used));
         singles.Sort((a, b) => {
            if (a.Prime2 > b.Prime2) return 1;
            else if (a.Prime2 == b.Prime2) return 0;
            else return -1;
         });

         int j = 0;
         while (j < singles.Count - 1)
         {
            if (singles[j].Prime2 == singles[j + 1].Prime2)
            {
               BigInteger qOfX = singles[j].QOfX * singles[j + 1].QOfX;
               BigInteger x = singles[j].X * singles[j + 1].X;
               BigBitArray exponentVector = new BigBitArray(singles[j].ExponentVector);
               exponentVector.Xor(singles[j + 1].ExponentVector);
               Relation newRelation = new(qOfX, x, exponentVector, RelationOrigin.OneLargePrime);
               _relations.Add(newRelation);
               singles[j + 1].Used = true;
               singles[j].Used = true;
               singles.RemoveAt(j);
            }
            j++;
         }
      }

      private void CombineComponent(long root, List<long> primes,
         Dictionary<long, List<PartialPartialRelation>> rbp)
      {
         Dictionary<long, Vertex> graphComponent = new(1 + primes.Count);

         // Add the root vertex to the Graph Component
         Vertex rootVertex = new Vertex(root);
         graphComponent.Add(root, rootVertex);

         // For each Edge that connects to the root
         List<PartialPartialRelation> edges = rbp[root];
         foreach (PartialPartialRelation edge in edges)
         {
            if (edge.Used)
               continue;

            rootVertex.Add(edge);

            // Add the child Vertex to the Graph Component.
            long otherPrime = edge.Prime1 == root ? edge.Prime2 : edge.Prime1;
            Vertex vertex = new Vertex(otherPrime, rootVertex, edge);
            graphComponent.Add(otherPrime, vertex);

            AddChildVertices(graphComponent, vertex, rbp);
         }
      }

      private void AddChildVertices(Dictionary<long, Vertex> graphComponent,
         Vertex parentVertex, Dictionary<long, List<PartialPartialRelation>> rbp)
      {
         List<PartialPartialRelation> childEdges = rbp[parentVertex.Prime];
         foreach (PartialPartialRelation ppr in childEdges)
         {
            if (ppr.Used)
               continue;

            long otherPrime = ppr.Prime1 == parentVertex.Prime ? ppr.Prime2 : ppr.Prime1;

            // Is the otherPrime already in the Graph Component?
            if (graphComponent.ContainsKey(otherPrime))
            {
               // We have found a cycle.  It consists of this edge (ppr), and
               // all the edges from both ends of ppr back to the common vertex.
               List<long> parentToRoot = new();
               Vertex? ancestor = parentVertex;
               while (ancestor is not null)
               {
                  parentToRoot.Add(ancestor.Prime);
                  ancestor = ancestor.Ancestor;
               }

               List<long> otherToRoot = new();
               ancestor = graphComponent[otherPrime];
               while (ancestor is not null && !parentToRoot.Contains(ancestor.Prime))
               {
                  otherToRoot.Add(ancestor.Prime);
                  ancestor = ancestor.Ancestor;
               }

               // If ancestor is null, the root of the component is the common Vertex.
               // If ancestor is not null, ancestor.Prime is the common Vertex.
               long commonVertexPrime;
               if (ancestor is null)
                  commonVertexPrime = _components[otherPrime];
               else
                  commonVertexPrime = ancestor.Prime;

               List<PartialPartialRelation> cycleRelations = new();
               cycleRelations.Add(ppr);  // Add the closing edge to the cycle.
               ppr.Used = true;          // ensure we don't re-use this edge.

               // Follow the parentVertex edges back to the common Vertex
               Vertex currentVertex = parentVertex;
               ancestor = currentVertex.Ancestor;
               while (ancestor is not null && currentVertex.Prime != commonVertexPrime)
               {
                  PartialPartialRelation nextEdge = currentVertex.Where(p => p.Prime1 == ancestor.Prime || p.Prime2 == ancestor.Prime).First();
                  cycleRelations.Add(nextEdge);
                  currentVertex = ancestor;
                  ancestor = ancestor.Ancestor;
               }

               // Follow the other Vertex's edges back to the common Vertex
               currentVertex = graphComponent[otherPrime];
               ancestor = currentVertex.Ancestor;
               while (ancestor is not null && currentVertex.Prime != commonVertexPrime)
               {
                  PartialPartialRelation nextEdge = currentVertex.Where(p => p.Prime1 == ancestor.Prime || p.Prime2 == ancestor.Prime).First();
                  cycleRelations.Add(nextEdge);
                  currentVertex = ancestor;
                  ancestor = ancestor.Ancestor;
               }

               // handle cycle
               BigInteger qOfX = BigInteger.One;
               BigInteger x = BigInteger.One;
               BigBitArray exponentVector = new BigBitArray(cycleRelations[0].ExponentVector.Capacity);
               foreach (PartialPartialRelation j in cycleRelations)
               {
                  qOfX *= j.QOfX;
                  x *= j.X;
                  exponentVector.Xor(j.ExponentVector);
               }

               // Since we merged as many Single Large Primes as we could before
               // graph operations, we know that at least one with Two Large Primes
               // was used.
               Relation newRelation = new(qOfX, x, exponentVector, RelationOrigin.TwoLargePrimes);
               _relations.Add(newRelation);
            }
            else
            {
               Vertex child = new Vertex(otherPrime, parentVertex, ppr);
               graphComponent.Add(otherPrime, child);
               AddChildVertices(graphComponent, child, rbp);
            }
         }
      }

      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         CombineComponents();

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

