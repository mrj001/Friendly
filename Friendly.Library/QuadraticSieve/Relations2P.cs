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
      /// This Dictionary maps each prime (the key) to the spanning tree containing
      /// it.
      /// </summary>
      /// <remarks>
      /// <para>
      /// A Union-Find algorithm is used to merge spanning trees.  The "root"
      /// of the tree is essentially chosen arbitrarily (the first Vertex found
      /// for that component).  The Key is any prime within the Graph.  The
      /// Value is the Vertex (Prime) chosen to be the root of that Spanning
      /// Tree.
      /// </para>
      /// </remarks>
      private readonly Dictionary<long, long> _spanningTrees;

      /// <summary>
      /// This Dictionary specifies the List of Edges (The Value) that are
      /// incident to each Vertex (the Key).
      /// </summary>
      private readonly Dictionary<long, List<PartialPartialRelation>> _relationsByPrime;
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

         // Create the Graph
         // We initialize the Graph with the special "prime" of one.
         // This way no special code is needed to add it when adding the first
         // Partial Relation.
         int initialCapacity = 1 << 23;
         _componentCount = 1;
         _spanningTrees = new Dictionary<long, long>(initialCapacity);
         _spanningTrees.Add(1, 1);

         _relationsByPrime = new Dictionary<long, List<PartialPartialRelation>>(initialCapacity);
         _relationsByPrime.Add(1, new List<PartialPartialRelation>());
      }

      /// <summary>
      /// Finds the "root" of the current set (component) of vertices.
      /// </summary>
      /// <param name="prime">The prime to find the root of.</param>
      /// <returns>The prime which is serving as the root of the given prime's component.</returns>
      private long Find(long prime)
      {
         // Is this prime already in the Graph?
         if (!_spanningTrees.ContainsKey(prime))
         {
            // Add the new component as its own root.
            _componentCount++;
            _spanningTrees.Add(prime, prime);
            return prime;
         }

         // Find the root of this component, keeping track of intermediate
         // ancestors.
         List<long> ancestors = new();
         long r = prime;
         while (_spanningTrees[r] != r)
         {
            ancestors.Add(r);
            r = _spanningTrees[r];
         }

         // Update all intermediate ancestors to point to the root.
         foreach (long p in ancestors)
            _spanningTrees[p] = r;

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
         foreach (long key in _spanningTrees.Keys)
            if (_spanningTrees[key] == r2)
               _spanningTrees[key] = r1;
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
         // Ensure that p1 is less than p2;
         if (p1 > p2)
         {
            long tmp = p1;
            p1 = p2;
            p2 = tmp;
         }

         bool p1Connected = _spanningTrees.ContainsKey(p1);
         bool p2Connected = _spanningTrees.ContainsKey(p2);

         // There are 4 possibilities for the values of p1Connected & p2Connected
         PartialPartialRelation newRelation = new PartialPartialRelation(QofX, x, exponentVector, p1, p2);
         if (p1Connected)
         {
            if (p2Connected)
            {
               long p1Root = Find(p1);
               long p2Root = Find(p2);
               if (p1Root == p2Root)
               {
                  // A Cycle has been found.
                  AddCycle(newRelation, p1, p2);
                  // NOTE: the newRelation is explicitly NOT added to the Spanning
                  // Tree.  Because this Edge is then guaranteed NOT to appear in
                  // any other cycles, we are generating Fundamental Cycles.
               }
               else
               {
                  Union(p1, p2);

                  List<PartialPartialRelation> lstP1 = _relationsByPrime[p1];
                  lstP1.Add(newRelation);

                  List<PartialPartialRelation> lstP2 = _relationsByPrime[p2];
                  lstP2.Add(newRelation);
               }
            }
            else
            {  // p2 is new
               // Add this new Vertex to the Spanning Tree containing p1.
               Union(p1, p2);

               // Add this Edge 
               List<PartialPartialRelation> lstP1 = _relationsByPrime[p1];
               lstP1.Add(newRelation);

               List<PartialPartialRelation> lstP2 = new List<PartialPartialRelation>();
               lstP2.Add(newRelation);
               _relationsByPrime.Add(p2, lstP2);
            }
         }
         else
         {
            if (p2Connected)
            {  // p1 is new
               // Add this new Vertex to the Spanning Tree containing p2.
               Union(p2, p1);

               // Add this Edge 
               List<PartialPartialRelation> lstP2 = _relationsByPrime[p2];
               lstP2.Add(newRelation); 

               List<PartialPartialRelation> lstP1 = new List<PartialPartialRelation>();
               lstP1.Add(newRelation);
               _relationsByPrime.Add(p1, lstP1);
            }
            else
            {
               // Both p1 and p2 are new primes
               // Add them as their own Spanning Tree
               Union(p1, p2);

               // Add the new Edge to both Vertices
               List<PartialPartialRelation> lstP1 = new List<PartialPartialRelation>();
               lstP1.Add(newRelation);
               _relationsByPrime.Add(p1, lstP1);

               List<PartialPartialRelation> lstP2 = new List<PartialPartialRelation>();
               lstP2.Add(newRelation);
               _relationsByPrime.Add(p2, lstP2);
            }
         }
      }

      /// <summary>
      /// Adds a cycle
      /// </summary>
      /// <param name="finalEdge">The Edge that closes the Cycle.</param>
      /// <param name="p1">The start Vertex</param>
      /// <param name="p2">The end Vertex</param>
      private void AddCycle(PartialPartialRelation finalEdge, long p1, long p2)
      {
         BigInteger qOfX = finalEdge.QOfX;
         BigInteger x = finalEdge.X;
         BigBitArray exponentVector = new BigBitArray(finalEdge.ExponentVector);
         bool twoLargePrimes = finalEdge.Prime1 != 1;

         List<PartialPartialRelation> cycle = FindPath(p1, p2);
         foreach(PartialPartialRelation edge in cycle)
         {
            qOfX *= edge.QOfX;
            x *= edge.X;
            exponentVector.Xor(edge.ExponentVector);
            twoLargePrimes |= edge.Prime1 != 1;
         }

         Relation newRelation = new Relation(qOfX, x, exponentVector,
            twoLargePrimes ? RelationOrigin.TwoLargePrimes : RelationOrigin.OneLargePrime);
         _relations.Add(newRelation);
      }

      /// <summary>
      /// Finds a Path through the connected component from Vertex p1 to
      /// Vertex p2.
      /// </summary>
      /// <param name="p1">The start Vertex</param>
      /// <param name="p2">The end Vertex</param>
      /// <returns>A List of Edges that form the Path.</returns>
      /// <remarks>
      /// <para>
      /// This uses a Breadth-First Search to find the path from p1 to p2.
      /// The order of the Edges in the List is from p2 to p1.
      /// </para>
      /// </remarks>
      private List<PartialPartialRelation> FindPath(long p1, long p2)
      {
         Queue<long> pendingVertices = new();
         // Key is the visited vertex.
         // Value is the Vertex from which the visit came.
         Dictionary<long, long> visitedVertices = new();

         visitedVertices.Add(p1, p1);
         pendingVertices.Enqueue(p1);

         while (pendingVertices.Count > 0)
         {
            long pCurrent = pendingVertices.Dequeue();

            List<PartialPartialRelation> outgoingEdges = _relationsByPrime[pCurrent];
            foreach (PartialPartialRelation outgoingEdge in outgoingEdges)
            {
               long pChild = outgoingEdge.Prime1 == pCurrent ? outgoingEdge.Prime2 : outgoingEdge.Prime1;

               if (pChild == p2)
               {
                  long parentVertex = pCurrent;
                  List<PartialPartialRelation> rv = new();
                  PartialPartialRelation edge = _relationsByPrime[pCurrent]
                     .Where(rel => (rel.Prime1 == pCurrent && rel.Prime2 == pChild) || (rel.Prime1 == pChild && rel.Prime2 == pCurrent))
                     .First();
                  rv.Add(edge);
                  while (visitedVertices[pCurrent] != pCurrent)
                  {
                     parentVertex = visitedVertices[pCurrent];
                     edge = _relationsByPrime[parentVertex]
                        .Where(rel => (rel.Prime1 == pCurrent && rel.Prime2 == parentVertex) || (rel.Prime1 == parentVertex && rel.Prime2 == pCurrent))
                        .First();
                     rv.Add(edge);
                     pCurrent = parentVertex;
                  }

                  return rv;
               }

               if (!visitedVertices.ContainsKey(pChild))
               {
                  visitedVertices.Add(pChild, pCurrent);
                  pendingVertices.Enqueue(pChild);
               }
            }
         }

         throw new ApplicationException();
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
         get => _relations.Count;
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

