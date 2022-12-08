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
      private const string PrimesNodeName = "primes";
      private const string PrimeNodeName = "prime";
      private const string OriginNodeName = "origin";

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

      public TPRelation(XmlNode relationNode)
      {
         XmlNode? qofxNode = relationNode.FirstChild;
         if (qofxNode is null || qofxNode.LocalName != QofXNodeName)
            throw new ArgumentException($"Failed to find <{QofXNodeName}>");
         _qofX = BigInteger.Parse(qofxNode.InnerText);

         XmlNode? xNode = qofxNode.NextSibling;
         if (xNode is null || xNode.LocalName != XNodeName)
            throw new ArgumentException($"Failed to find <{XNodeName}>.");
         _x = BigInteger.Parse(xNode.InnerText);

         XmlNode? expVectorNode = xNode.NextSibling;
         if (expVectorNode is null || expVectorNode.LocalName != ExponentVectorNodeName)
            throw new ArgumentException($"Failed to find <{ExponentVectorNodeName}>.");
         _exponentVector = new BigBitArray(expVectorNode);

         XmlNode? primesNode = expVectorNode.NextSibling;
         if (primesNode is null || primesNode.LocalName != PrimesNodeName)
            throw new ArgumentException($"Failed to find <{PrimesNodeName}>.");
         List<long> primes = new(3);
         XmlNode? primeNode = primesNode.FirstChild;
         while (primeNode is not null)
         {
            long p;
            if (!long.TryParse(primeNode.InnerText, out p))
               throw new ArgumentException($"Failed to parse '{primeNode.InnerText}' for <{PrimeNodeName}>");
            primes.Add(p);
            primeNode = primeNode.NextSibling;
         }
         _primes = primes.ToArray();

         XmlNode? originNode = primesNode.NextSibling;
         if (originNode is not null && originNode.LocalName == OriginNodeName)
         {
            _origin = (RelationOrigin)Enum.Parse(typeof(RelationOrigin), originNode.InnerText);
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
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         // Nothing to do here.
      }

      /// <inheritdoc />
      public XmlNode Serialize(XmlDocument doc, string name)
      {
         XmlNode rv = doc.CreateElement(name);

         XmlNode qofxNode = doc.CreateElement(QofXNodeName);
         qofxNode.InnerText = _qofX.ToString();
         rv.AppendChild(qofxNode);

         XmlNode xNode = doc.CreateElement(XNodeName);
         xNode.InnerText = _x.ToString();
         rv.AppendChild(xNode);

         rv.AppendChild(_exponentVector.Serialize(doc, ExponentVectorNodeName));

         XmlNode primesNode = doc.CreateElement(PrimesNodeName);
         rv.AppendChild(primesNode);
         foreach(long p in _primes)
         {
            XmlNode primeNode = doc.CreateElement(PrimeNodeName);
            primeNode.InnerText = p.ToString();
            primesNode.AppendChild(primeNode);
         }

         XmlNode originNode = doc.CreateElement(OriginNodeName);
         originNode.InnerText = _origin.ToString();
         rv.AppendChild(originNode);

         return rv;
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

