#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Friendly.Library.QuadraticSieve
{
   public class TPRelation : IList<long>, IEquatable<TPRelation>
   {
      private readonly BigInteger _qofX;
      private readonly BigInteger _x;
      private readonly BigBitArray _exponentVector;
      private readonly long[] _primes;
      private readonly RelationOrigin _origin;

      public TPRelation(BigInteger qofX, BigInteger x,
         BigBitArray exponentVector, long[] primes)
      {
         _qofX = qofX;
         _x = x;
         _exponentVector = exponentVector;
         _primes = primes;

         switch(primes.Length)
         {
            case 1:
               _origin = RelationOrigin.OneLargePrime;
               break;

            case 2:
               _origin = RelationOrigin.TwoLargePrimes;
               break;

            case 3:
               _origin = RelationOrigin.ThreeLargePrimes;
               break;
         }
      }

      public TPRelation(BigInteger qofX, BigInteger x,
         BigBitArray exponentVector, long[] primes, RelationOrigin origin)
      {
         _qofX = qofX;
         _x = x;
         _exponentVector = exponentVector;
         _primes = primes;
         _origin = origin;
      }

      public BigInteger QOfX { get => _qofX; }
      public BigInteger X { get => _x; }
      public BigBitArray ExponentVector { get => _exponentVector; }
      public RelationOrigin Origin { get => _origin; }

      #region Object Overrides
      public override bool Equals(object? obj)
      {
         return Equals(obj as PartialPartialRelation);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            int rv = 0;
            foreach (long p in _primes)
              rv ^= 47 * (int)p;
            return rv;
         }
      }
      #endregion

      #region IEquatable<TPRelation>
      public bool Equals(TPRelation? other)
      {
         if (other is null)
            return false;

         if (_primes.Length != other._primes.Length)
            return false;

         for (int j = 0; j < _primes.Length; j++)
            if (_primes[j] != other._primes[j])
               return false;

         // Note that the exponent vector is a function of things we've
         // already compared, so we don't check it.
         return _qofX == other.QOfX && X == other.X;
      }

      public static bool operator==(TPRelation? l, TPRelation? r)
      {
         if (l is not null)
            return l.Equals(r);
         else
            return r is not null;
      }

      public static bool operator !=(TPRelation? l, TPRelation? r)
      {
         return !(l == r);
      }
      #endregion

      #region IList<long>
      public long this[int index]
      {
         get => _primes[index];
         set => throw new NotSupportedException();
      }

      public int Count { get => _primes.Length; }

      public bool IsReadOnly { get => true; }

      public void Add(long item)
      {
         throw new NotSupportedException();
      }

      public void Clear()
      {
         throw new NotSupportedException();
      }

      public bool Contains(long item)
      {
         for (int j = 0; j < _primes.Length; j++)
            if (_primes[j] == item)
               return true;

         return false;
      }

      public void CopyTo(long[] array, int arrayIndex)
      {
         throw new NotImplementedException();
      }

      public int IndexOf(long item)
      {
         for (int j = 0; j < _primes.Length; j++)
            if (_primes[j] == item)
               return j;

         return -1;
      }

      public void Insert(int index, long item)
      {
         throw new NotSupportedException();
      }

      public bool Remove(long item)
      {
         throw new NotSupportedException();
      }

      public void RemoveAt(int index)
      {
         throw new NotSupportedException();
      }

      public IEnumerator<long> GetEnumerator()
      {
         IEnumerator iterator =  _primes.GetEnumerator();
         while (iterator.MoveNext())
            yield return (long)iterator.Current;
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return _primes.GetEnumerator();
      }
      #endregion
   }
}

