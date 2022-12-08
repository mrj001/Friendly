#nullable enable
using System;
using System.Xml;
using System.Numerics;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   public class PartialPartialRelation : IEquatable<PartialPartialRelation>, ISerialize
   {
      private BigInteger _qofX;
      private BigInteger _x;
      private BigBitArray _exponentVector;
      private long _p1;
      private long _p2;

      private const string QofXNodeName = "qofx";
      private const string XNodeName = "x";
      private const string ExponentVectorNodeName = "exponentvector";
      private const string PrimesNodeName = "primes";
      private const string PrimeNodeName = "prime";

      public PartialPartialRelation(BigInteger qofX, BigInteger x,
         BigBitArray exponentVector, long p1, long p2)
      {
         _qofX = qofX;
         _x = x;
         _exponentVector = exponentVector;
         _p1 = p1;
         _p2 = p2;
      }

      public PartialPartialRelation(XmlNode relationNode)
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
         XmlNode? primeNode = primesNode.FirstChild;
         if (primeNode is null || primeNode.LocalName != PrimeNodeName)
            throw new ArgumentException($"Failed to find first <{PrimeNodeName}>.");
         if (!long.TryParse(primeNode.InnerText, out _p1))
            throw new ArgumentException($"Failed to parse '{primeNode.InnerText}' for <{PrimeNodeName}>");
         primeNode = primeNode.NextSibling;
         if (primeNode is null || primeNode.LocalName != PrimeNodeName)
            throw new ArgumentException($"Failed to find second <{PrimeNodeName}>.");
         if (!long.TryParse(primeNode.InnerText, out _p2))
            throw new ArgumentException($"Failed to parse '{primeNode.InnerText}' for <{PrimeNodeName}>");
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

         XmlNode primesNode = doc.CreateElement( PrimesNodeName);
         rv.AppendChild(primesNode);

         XmlNode primeNode = doc.CreateElement(PrimeNodeName);
         primeNode.InnerText = _p1.ToString();
         primesNode.AppendChild(primeNode);
         primeNode = doc.CreateElement(PrimeNodeName);
         primeNode.InnerText = _p2.ToString();
         primesNode.AppendChild(primeNode);

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
      public long Prime1 { get => _p1; }
      public long Prime2 { get => _p2; }

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