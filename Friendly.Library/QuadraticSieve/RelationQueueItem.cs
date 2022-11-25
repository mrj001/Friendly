using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// Represents a queued item, which is waiting to be processed into
   /// the Relations graph.
   /// </summary>
   public class RelationQueueItem
   {
      public RelationQueueItem(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         BigInteger residual)
      {
         this.QofX = QofX;
         this.X = x;
         this.ExponentVector = exponentVector;
         this.Residual = residual;
      }

      public BigInteger QofX { get; }
      public BigInteger X { get; }
      public BigBitArray ExponentVector { get; }
      public BigInteger Residual { get; }
   }
}

