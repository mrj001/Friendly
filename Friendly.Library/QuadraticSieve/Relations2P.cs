#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using Friendly.Library.Pollard;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// An implementation of the IRelations interface to be used with the Two
   /// Large Primes variation.
   /// </summary>
   public class Relations2P : IRelations
   {
      #region Member Data
      private BufferBlock<RelationQueueItem>? _queue;
      private ActionBlock<RelationQueueItem>? _actionFactor;
      private ActionBlock<PartialPartialRelation>? _actionCycle;
      private int _maxQueueLength;

      private int _maxCycleLength = 0;

      /// <summary>
      /// The set of fully factored Relations found during sieving.
      /// </summary>
      private readonly List<Relation> _relations;
      private readonly object _lockRelations = new object();

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
      private readonly object _lockRelationsByPrime = new object();

      private const int InitialCapacity = 1 << 18;
      public const string TypeNodeName = "type";
      private const string LargePrimeNodeName = "maxLargePrime";
      private const string TwoLargePrimeNodeName = "maxTwoLargePrimes";
      private const string StatisticsNodeName = "statistics";
      public const string StatisticNodeName = "statistic";
      private const string MaxQueueLengthStatName = "maxqueuelength";
      private const string MaxCycleLengthStatName = "maxcyclelength";
      public const string RelationsNodeName = "relations";
      public const string PartialRelationsNodeName = "partialrelations";
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
         // Set up the background task to process items from the Relations Queue.
         StartBackground();
         _maxQueueLength = 0;

         _relations = new();
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;
         _maxLargePrime = maxLargePrime;

         _maxTwoPrimes = _maxLargePrime > int.MaxValue ? long.MaxValue : _maxLargePrime * _maxLargePrime;

         // Create the Graph
         // We initialize the Graph with the special "prime" of one.
         // This way no special code is needed to add it when adding the first
         // Partial Relation.
         _componentCount = 1;
         _spanningTrees = new Dictionary<long, long>(InitialCapacity);
         _spanningTrees.Add(1, 1);

         _relationsByPrime = new Dictionary<long, List<PartialPartialRelation>>(InitialCapacity);
         _relationsByPrime.Add(1, new List<PartialPartialRelation>());
      }

      public Relations2P(int factorBaseSize, int maxFactor, XmlReader rdr)
      {
         _factorBaseSize = factorBaseSize;
         _maxFactor = maxFactor;

         rdr.ReadStartElement(LargePrimeNodeName);
         _maxLargePrime = SerializeHelper.ParseLongNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(TwoLargePrimeNodeName);
         _maxTwoPrimes = SerializeHelper.ParseLongNode(rdr);
         rdr.ReadEndElement();

         // Restore stats that we need to round trip
         List<Statistic> statistics = new();
         rdr.ReadStartElement(StatisticsNodeName);
         while (rdr.IsStartElement(StatisticNodeName))
            statistics.Add(new Statistic(rdr));
         rdr.ReadEndElement();
         _maxQueueLength = (int)(statistics.Where(s => s.Name == MaxQueueLengthStatName).First().Value);
         _maxCycleLength = (int)(statistics.Where(s => s.Name == MaxCycleLengthStatName).First().Value);

         // Read the full Relations
         _relations = new();
         rdr.ReadStartElement(RelationsNodeName);
         while (rdr.IsStartElement("r"))
            _relations.Add(new Relation(rdr));
         rdr.ReadEndElement();

         // Create the Graph
         _componentCount = 1;
         _spanningTrees = new Dictionary<long, long>(InitialCapacity);
         _spanningTrees.Add(1, 1);
         _relationsByPrime = new Dictionary<long, List<PartialPartialRelation>>(InitialCapacity);
         _relationsByPrime.Add(1, new List<PartialPartialRelation>());

         // Read the Partial Relations, and construct the Graph
         rdr.ReadStartElement(PartialRelationsNodeName);
         while (rdr.Read() && rdr.NodeType == XmlNodeType.Element)
         {
            PartialPartialRelation ppr = new PartialPartialRelation(rdr);

            // Only Partial Partial Relations that were part of a spanning tree
            // were saved, so no cycle detection required.
            Union(ppr.Prime1, ppr.Prime2);

            List<PartialPartialRelation>? lst;
            if (!_relationsByPrime.TryGetValue(ppr.Prime1, out lst))
            {
               lst = new();
               _relationsByPrime.Add(ppr.Prime1, lst);
            }
            lst.Add(ppr);

            if (!_relationsByPrime.TryGetValue(ppr.Prime2, out lst))
            {
               lst = new();
               _relationsByPrime.Add(ppr.Prime2, lst);
            }
            lst.Add(ppr);
         }
         rdr.ReadEndElement();

         // Set up the background task to process items from the Relations Queue.
         StartBackground();

         rdr.ReadEndElement();
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // Shut down the background task, so it is safe to serialize this
         // instance.
         StopBackground();
      }

      /// <inheritdoc />
      public XmlNode Serialize(XmlDocument doc, string name)
      {
         XmlNode rv = doc.CreateElement(name);

         XmlNode typeNode = doc.CreateElement(TypeNodeName);
         typeNode.InnerText = "Relations2P";
         rv.AppendChild(typeNode);

         SerializeHelper.AddLongNode(doc, rv, LargePrimeNodeName, _maxLargePrime);
         SerializeHelper.AddLongNode(doc, rv, TwoLargePrimeNodeName, _maxTwoPrimes);

         XmlNode statsNode = doc.CreateElement(StatisticsNodeName);
         rv.AppendChild(statsNode);
         Statistic statistic = new Statistic(MaxQueueLengthStatName, _maxQueueLength);
         statsNode.AppendChild(statistic.Serialize(doc, StatisticNodeName));
         statistic = new Statistic(MaxCycleLengthStatName, _maxCycleLength);
         statsNode.AppendChild(statistic.Serialize(doc, StatisticNodeName));

         XmlNode relationsNode = doc.CreateElement(RelationsNodeName);
         rv.AppendChild(relationsNode);
         foreach(Relation r in _relations)
            relationsNode.AppendChild(r.Serialize(doc, "r"));

         // We need to build a set of all the unique Partial Partial Relations
         // that are in the Graph.
         HashSet<PartialPartialRelation> pprs = new HashSet<PartialPartialRelation>(InitialCapacity);
         foreach(List<PartialPartialRelation> lst in _relationsByPrime.Values)
            foreach(PartialPartialRelation ppr in lst)
               if (!pprs.Contains(ppr))
                  pprs.Add(ppr);

         // Output the unique set
         XmlNode partialRelationsNode = doc.CreateElement(PartialRelationsNodeName);
         rv.AppendChild(partialRelationsNode);
         foreach(PartialPartialRelation ppr in pprs)
            partialRelationsNode.AppendChild(ppr.Serialize(doc, "r"));

         return rv;
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         if (reason == SerializationReason.SaveState)
            StartBackground();
      }

      private void StartBackground()
      {
         _queue = new BufferBlock<RelationQueueItem>();
         _actionFactor = new ActionBlock<RelationQueueItem>((rqi) => TryAddRelation(rqi));
         _actionCycle = new ActionBlock<PartialPartialRelation>(tpr => AddCycle(tpr));

         _queue.LinkTo(_actionFactor);
         _queue.Completion.ContinueWith(delegate { _actionFactor.Complete(); });
         _actionFactor.Completion.ContinueWith(delegate { _actionCycle.Complete(); });
      }

      private void StopBackground()
      {
         _queue?.Complete();
         _actionCycle?.Completion.Wait();
         _queue = null;
         _actionFactor = null;
         _actionCycle = null;
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
         _spanningTrees[r2] = r1;
      }

      /// <inheritdoc />
      public void TryAddRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         BigInteger residual)
      {
         RelationQueueItem item = new RelationQueueItem(QofX, x, exponentVector, residual);
         _queue!.Post(item);
         int queueLen = _queue!.Count;
         _maxQueueLength = Math.Max(queueLen, _maxQueueLength);
      }

      private void TryAddRelation(RelationQueueItem item)
      {
         if (item.Residual == BigInteger.One)
         {
            lock(_lockRelations)
               _relations.Add(new Relation(item.QofX, item.X, item.ExponentVector));
         }
         else if (item.Residual < _maxLargePrime && item.Residual > _maxFactor)
         {
            AddPartialRelation(item.QofX, item.X, item.ExponentVector, 1, (long)item.Residual);
         }
         else if (item.Residual < _maxTwoPrimes && !Primes.IsPrime(item.Residual))
         {
            PollardRho rho = new PollardRho();
            (BigInteger p1, BigInteger p2) = rho.Factor(item.Residual);
            if (p1 != p2)
               AddPartialRelation(item.QofX, item.X, item.ExponentVector, (long)p1, (long)p2);
            else
               lock (_lockRelations)
                  _relations.Add(new Relation(item.QofX, item.X, item.ExponentVector, RelationOrigin.TwoLargePrimes));
         }
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
                  _actionCycle!.Post(newRelation);
                  // NOTE: the newRelation is explicitly NOT added to the Spanning
                  // Tree.  Because this Edge is then guaranteed NOT to appear in
                  // any other cycles, we are generating Fundamental Cycles.
               }
               else
               {
                  Union(p1, p2);

                  lock (_lockRelationsByPrime)
                  {
                     List<PartialPartialRelation> lstP1 = _relationsByPrime[p1];
                     lstP1.Add(newRelation);

                     List<PartialPartialRelation> lstP2 = _relationsByPrime[p2];
                     lstP2.Add(newRelation);
                  }
               }
            }
            else
            {  // p2 is new
               // Add this new Vertex to the Spanning Tree containing p1.
               Union(p1, p2);

               // Add this Edge 
               lock (_lockRelationsByPrime)
               {
                  List<PartialPartialRelation> lstP1 = _relationsByPrime[p1];
                  lstP1.Add(newRelation);

                  List<PartialPartialRelation> lstP2 = new List<PartialPartialRelation>();
                  lstP2.Add(newRelation);
                  _relationsByPrime.Add(p2, lstP2);
               }
            }
         }
         else
         {
            if (p2Connected)
            {  // p1 is new
               // Add this new Vertex to the Spanning Tree containing p2.
               Union(p2, p1);

               lock (_lockRelationsByPrime)
               {
                  // Add this Edge 
                  List<PartialPartialRelation> lstP2 = _relationsByPrime[p2];
                  lstP2.Add(newRelation);

                  List<PartialPartialRelation> lstP1 = new List<PartialPartialRelation>();
                  lstP1.Add(newRelation);
                  _relationsByPrime.Add(p1, lstP1);
               }
            }
            else
            {
               // Both p1 and p2 are new primes
               // Add them as their own Spanning Tree
               Union(p1, p2);

               lock (_lockRelationsByPrime)
               {
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
      }

      /// <summary>
      /// Adds a cycle
      /// </summary>
      /// <param name="finalEdge">The Edge that closes the Cycle.</param>
      private void AddCycle(PartialPartialRelation finalEdge)
      {
         long p1 = finalEdge.Prime1;
         long p2 = finalEdge.Prime2;
         BigInteger qOfX = finalEdge.QOfX;
         BigInteger x = finalEdge.X;
         BigBitArray exponentVector = new BigBitArray(finalEdge.ExponentVector);
         bool twoLargePrimes = finalEdge.Prime1 != 1;

         List<PartialPartialRelation> cycle = FindPath(p1, p2);
         _maxCycleLength = Math.Max(_maxCycleLength, cycle.Count);
         foreach(PartialPartialRelation edge in cycle)
         {
            qOfX *= edge.QOfX;
            x *= edge.X;
            exponentVector.Xor(edge.ExponentVector);
            twoLargePrimes |= edge.Prime1 != 1;
         }

         Relation newRelation = new Relation(qOfX, x, exponentVector,
            twoLargePrimes ? RelationOrigin.TwoLargePrimes : RelationOrigin.OneLargePrime);
         lock (_lockRelations)
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

            lock (_lockRelationsByPrime)
            {
               List<PartialPartialRelation> outgoingEdges = _relationsByPrime[pCurrent];
               foreach (PartialPartialRelation outgoingEdge in outgoingEdges)
               {
                  long pChild = outgoingEdge.Prime1 == pCurrent ? outgoingEdge.Prime2 : outgoingEdge.Prime1;

                  if (pChild == p2)
                  {
                     List<PartialPartialRelation> rv = new();
                     visitedVertices.Add(pChild, pCurrent);
                     long parentVertex = pCurrent;
                     pCurrent = pChild;
                     do
                     {
                        PartialPartialRelation edge = _relationsByPrime[parentVertex]
                           .Where(rel => (rel.Prime1 == pCurrent && rel.Prime2 == parentVertex) || (rel.Prime1 == parentVertex && rel.Prime2 == pCurrent))
                           .First();
                        rv.Add(edge);
                        pCurrent = parentVertex;
                        parentVertex = visitedVertices[pCurrent];
                     } while (parentVertex != pCurrent);

                     return rv;
                  }

                  if (!visitedVertices.ContainsKey(pChild))
                  {
                     visitedVertices.Add(pChild, pCurrent);
                     pendingVertices.Enqueue(pChild);
                  }
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
            lock (_lockRelations)
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
         lock (_lockRelations)
            _relations.RemoveAt(index);
      }

      /// <inheritdoc />
      public void PrepareToResieve()
      {
         StartBackground();
      }

      /// <inheritdoc />
      public int Count
      {
         get
         {
            lock (_lockRelations)
               return _relations.Count + (_actionCycle?.InputCount ?? 0);
         }
      }

      public IMatrix GetMatrix(IMatrixFactory matrixFactory)
      {
         StopBackground();
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

         List<Statistic> rv = new();
         rv.Add(new Statistic(StatisticNames.FullyFactored, counts[0]));
         rv.Add(new Statistic(StatisticNames.OneLargePrime, counts[1]));
         rv.Add(new Statistic(StatisticNames.TwoLargePrimes, counts[2]));
         rv.Add(new Statistic("Components", _componentCount));
         rv.Add(new Statistic("DictionaryLoad", ((float)_spanningTrees.Count) / _spanningTrees.EnsureCapacity(0)));
         rv.Add(new Statistic("MaxQueueLength", _maxQueueLength));
         rv.Add(new Statistic("MaxCycleLength", _maxCycleLength));
         return rv.ToArray();
      }
   }
}
