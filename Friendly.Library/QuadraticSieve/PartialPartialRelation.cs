#nullable enable
using System;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class PartialPartialRelation : IEquatable<PartialPartialRelation>
   {
      private BigInteger _qofX;
      private BigInteger _x;
      private BigBitArray _exponentVector;
      private long _p1;
      private long _p2;
      private bool _used;

      public PartialPartialRelation(BigInteger qofX, BigInteger x,
         BigBitArray exponentVector, long p1, long p2)
      {
         _qofX = qofX;
         _x = x;
         _exponentVector = exponentVector;
         _p1 = p1;
         _p2 = p2;
         _used = false;
      }

      public BigInteger QOfX { get => _qofX; }
      public BigInteger X { get => _x; }
      public BigBitArray ExponentVector { get => _exponentVector; }
      public long Prime1 { get => _p1; }
      public long Prime2 { get => _p2; }

      /// <summary>
      /// Gets or sets whether a Relation has been "used".
      /// </summary>
      /// <remarks>
      /// <para>
      /// To prevent creation of excessive linear dependencies, one edge from
      /// each cycle will be marked as "used".  Such edge will not be eligible
      /// for use in subsequent cycles.
      /// </para>
      /// </remarks>
      public bool Used
      {
         get => _used;
         set => _used = value;
      }

      #region Object Overrides
      public override bool Equals(object? obj)
      {
         return Equals(obj as PartialPartialRelation);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            return (int)((47 * _p1) ^ (31 * _p2));
         }
      }
      #endregion

      #region IEquatable<PartialPartialRelation> & related

      public bool Equals(PartialPartialRelation? other)
      {
         if (other is null)
            return false;

         // Note: As _exponentVector, _p1 & _p2 are functions of _qofX and _x,
         // we need only compare these two fields.
         return _qofX == other.QOfX && _x == other.X;
      }

      public static bool operator ==(PartialPartialRelation? l, PartialPartialRelation? r)
      {
         if (l is not null)
            return l.Equals(r);
         else if (r is not null)
            return r.Equals(l);
         else
            return false;
      }

      public static bool operator !=(PartialPartialRelation? l, PartialPartialRelation? r)
      {
         return !(l == r);
      }
      #endregion
   }
}