using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class PartialRelation
   {
      private readonly BigInteger _qOfX;
      private readonly BigInteger _x;
      private readonly BigBitArray _exponentVector;
      private readonly long _largePrime;

      public PartialRelation(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         long largePrime)
      {
         _qOfX = QofX;
         _x = x;
         _exponentVector = exponentVector;
         _largePrime = largePrime;
      }

      public BigInteger QOfX { get => _qOfX; }
      public BigInteger X { get => _x; }
      public BigBitArray ExponentVector { get => _exponentVector; }

      /// <summary>
      /// 
      /// </summary>
      /// <remarks>
      /// <para>
      /// This may not be prime, but will be mutually prime with all primes in
      /// the Factor Base.
      /// </para>
      /// </remarks>
      public long LargePrime { get => _largePrime; }
   }
}

