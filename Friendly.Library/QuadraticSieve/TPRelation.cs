#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   public class TPRelation : IList<long>, IEquatable<TPRelation>, ISerialize
   {
      private readonly BigInteger _qofX;
      private readonly BigInteger _x;
      private readonly BigBitArray _exponentVector;
      private readonly long[] _primes;
      private readonly RelationOrigin _origin;

      private const string QofXNodeName = "qofx";
      private const string XNodeName = "x";
      private const string ExponentVectorNodeName = "exponentvector";
      public const string PrimesNodeName = "primes";
      public const string PrimeNodeName = "prime";
      public const string OriginNodeName = "origin";

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

      public TPRelation(XmlReader rdr)
      {
         rdr.ReadStartElement();

         rdr.ReadStartElement(QofXNodeName);
         _qofX = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(XNodeName);
         _x = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         _exponentVector = new BigBitArray(rdr);

         rdr.ReadStartElement(PrimesNodeName);
         List<long> primes = new(3);
         while (rdr.IsStartElement(PrimeNodeName))
         {
            rdr.ReadStartElement(PrimeNodeName);
            long p = SerializeHelper.ParseLongNode(rdr);
            rdr.ReadEndElement();
            primes.Add(p);
         }
         _primes = primes.ToArray();
         rdr.ReadEndElement();

         if (rdr.IsStartElement(OriginNodeName))
         {
            rdr.ReadStartElement(OriginNodeName);
            string innerText = rdr.ReadContentAsString();
            _origin = (RelationOrigin)Enum.Parse(typeof(RelationOrigin), innerText);
            rdr.ReadEndElement();
         }
         else
         {
            switch (_primes.Length)
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

         rdr.ReadEndElement();
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // Nothing to do here.
      }

      /// <inheritdoc />
      public void Serialize(XmlWriter writer, string name)
      {
         writer.WriteStartElement(name);

         writer.WriteElementString(QofXNodeName, _qofX.ToString());
         writer.WriteElementString(XNodeName, _x.ToString());
         _exponentVector.Serialize(writer, ExponentVectorNodeName);

         writer.WriteStartElement(PrimesNodeName);
         foreach(long p in _primes)
            writer.WriteElementString(PrimeNodeName, p.ToString());
         writer.WriteEndElement();

         writer.WriteElementString(OriginNodeName, _origin.ToString());

         writer.WriteEndElement();
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         // Nothing to do here.
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

