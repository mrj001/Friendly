#nullable enable
using System;
using System.Numerics;
using System.Xml;

namespace Friendly.Library.Utility
{
   public static class SerializeHelper
   {
      public static void ValidateNode(XmlNode? node, string name)
      {
         if (node is null || node.LocalName != name)
            throw new ArgumentException($"Failed to find <{name}>.");
      }

      public static int ParseIntNode(XmlNode node)
      {
         int rv;
         if (!int.TryParse(node.InnerText, out rv))
            throw new ArgumentException($"Failed to parse '{node.InnerText}' for <{node.LocalName}>.");
         return rv;
      }

      public static long ParseLongNode(XmlNode node)
      {
         long rv;
         if (!long.TryParse(node.InnerText, out rv))
            throw new ArgumentException($"Failed to parse '{node.InnerText}' for <{node.LocalName}>.");
         return rv;
      }

      public static BigInteger ParseBigIntegerNode(XmlNode node)
      {
         BigInteger rv;
         if (!BigInteger.TryParse(node.InnerText, out rv))
            throw new ArgumentException($"Failed to parse '{node.InnerText}' for <{node.LocalName}>.");
         return rv;
      }

      public static XmlNode AddIntNode(XmlDocument doc, XmlNode parent, string name, int value)
      {
         XmlNode rv = doc.CreateElement(name);
         rv.InnerText = value.ToString();
         parent.AppendChild(rv);
         return rv;
      }

      public static XmlNode AddLongNode(XmlDocument doc, XmlNode parent, string name, long value)
      {
         XmlNode rv = doc.CreateElement(name);
         rv.InnerText = value.ToString();
         parent.AppendChild(rv);
         return rv;
      }

      public static XmlNode AddBigIntegerNode(XmlDocument doc, XmlNode parent, string name, BigInteger value)
      {
         XmlNode rv = doc.CreateElement(name);
         rv.InnerText = value.ToString();
         parent.AppendChild(rv);
         return rv;
      }
   }
}

