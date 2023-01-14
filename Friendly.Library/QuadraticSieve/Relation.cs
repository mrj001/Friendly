using System;
using System.Numerics;
using System.Xml;
using Friendly.Library.Utility;

namespace Friendly.Library.QuadraticSieve
{
   public enum RelationOrigin : int
   {
      /// <summary>
      /// Indicates that the Relation was fully factored during sieving.
      /// </summary>
      FullyFactored = 0,

      /// <summary>
      /// Indicates that the Relation was formed by combining two Partial Relations.
      /// </summary>
      OneLargePrime = 1,

      /// <summary>
      /// Indicates that at least one of the combined Relations had two large primes.
      /// No combined Relations contained three large primes.
      /// </summary>
      TwoLargePrimes = 2,

      /// <summary>
      /// Indicates that at least one of the combined Relations had three large primes.
      /// </summary>
      ThreeLargePrimes = 3
   }

   public class Relation : ISerialize
   {
      private readonly BigInteger _qOfX;
      private readonly BigInteger _x;
      private readonly BigBitArray _exponentVector;
      private readonly RelationOrigin _origin;

      private const string QofXNodeName = "qofx";
      private const string XNodeName = "x";
      private const string ExponentVectorNodeName = "exponentvector";
      private const string OriginNodeName = "origin";

      /// <summary>
      /// Constructs a Relation object that was fully factored during sieving.
      /// </summary>
      /// <param name="QofX"></param>
      /// <param name="x"></param>
      /// <param name="exponentVector"></param>
      public Relation(BigInteger QofX, BigInteger x, BigBitArray exponentVector)
      {
         _qOfX = QofX;
         _x = x;
         _exponentVector = exponentVector;
         _origin = RelationOrigin.FullyFactored;
      }

      /// <summary>
      /// Constructs a Relation object where at least one Partial Partial
      /// Relation was used in determining the values.
      /// </summary>
      /// <param name="QofX"></param>
      /// <param name="x"></param>
      /// <param name="exponentVector"></param>
      /// <param name="origin"></param>
      public Relation(BigInteger QofX, BigInteger x, BigBitArray exponentVector,
         RelationOrigin origin)
      {
         _qOfX = QofX;
         _x = x;
         _exponentVector = exponentVector;
         _origin = origin;
      }

      /// <summary>
      /// Constructs a Relation object from two Relations containing a single
      /// large prime each.
      /// </summary>
      /// <param name="r1">The first Partial Relation</param>
      /// <param name="r2">The second Partial Relation</param>
      public Relation(PartialRelation r1, PartialRelation r2)
      {
         _qOfX = r1.QOfX * r2.QOfX;
         _x = r1.X * r2.X;
         _exponentVector = new BigBitArray(r1.ExponentVector);
         _exponentVector.Xor(r2.ExponentVector);
         _origin = RelationOrigin.OneLargePrime;
      }

      /// <summary>
      /// Deserializes a new Relation object from XML.
      /// </summary>
      /// <param name="rdr"></param>
      public Relation(XmlReader rdr)
      {
         rdr.ReadStartElement();
         rdr.ReadStartElement(QofXNodeName);
         _qOfX = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(XNodeName);
         _x = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         _exponentVector = new BigBitArray(rdr);

         rdr.ReadStartElement(OriginNodeName);
         string innerText = rdr.ReadContentAsString();
         _origin = (RelationOrigin)Enum.Parse(typeof(RelationOrigin), innerText);
         rdr.ReadEndElement();

         // End of Relation.
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

         writer.WriteElementString(QofXNodeName, _qOfX.ToString());
         writer.WriteElementString(XNodeName, _x.ToString());
         _exponentVector.Serialize(writer, ExponentVectorNodeName);
         writer.WriteElementString(OriginNodeName, _origin.ToString());

         writer.WriteEndElement();
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         // Nothing to do here.
      }

      public BigInteger QOfX { get => _qOfX; }
      public BigInteger X { get => _x; }
      public BigBitArray ExponentVector { get => _exponentVector; }
      public RelationOrigin Origin { get => _origin; }
   }
}

