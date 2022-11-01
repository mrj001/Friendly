using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class Relation
   {
      private readonly BigInteger _qOfX;
      private readonly BigInteger _x;
      private readonly BigBitArray _exponentVector;

      public Relation(BigInteger QofX, BigInteger x, BigBitArray exponentVector)
      {
         _qOfX = QofX;
         _x = x;
         _exponentVector = exponentVector;
      }

      public BigInteger QOfX { get => _qOfX; }
      public BigInteger X { get => _x; }
      public BigBitArray ExponentVector { get => _exponentVector; }
   }
}

