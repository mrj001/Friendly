using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Friendly.Library.QuadraticSieve;
using Friendly.Library.Utility;

namespace ShowProgress;

public class Program
{
   public static void Main(string[] args)
   {
      string? filename;

      if (args.Length > 0)
      {
         filename = args[0];
      }
      else
      {
         Console.Write("Enter filename: ");
         filename = Console.ReadLine();
      }

      if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
      {
         Console.Error.WriteLine("File not found.");
         return;
      }

      using (Stream strm = new FileStream(filename, FileMode.Open, FileAccess.Read))
      using (GZipStream gz = new GZipStream(strm, CompressionMode.Decompress))
      using (XmlReader rdr = XmlReader.Create(gz))
         ShowStats(rdr);
   }

   private static void ShowStats(XmlReader rdr)
   {
      PrintSimpleNode(rdr, QuadraticSieve.NumberNodeName);
      PrintSimpleNode(rdr, QuadraticSieve.MultiplierNodeName);
      PrintSimpleNode(rdr, QuadraticSieve.SieveIntervalNodeName);
      PrintSimpleNode(rdr, QuadraticSieve.FactorBaseSizeNodeName);

      PrintStatistics(rdr);
      PrintRelations(rdr);
   }

   private static void AdvanceToNode(XmlReader rdr, string name)
   {
      while (rdr.Read() && (rdr.NodeType != XmlNodeType.Element || rdr.LocalName != name))
         ;

      if (rdr.NodeType != XmlNodeType.Element || rdr.LocalName != name)
         throw new ArgumentException($"Failed to find node <{name}>.");
   }

   private static bool AdvanceToPossibleNode(XmlReader rdr, string name, string endname)
   {
      // end loop when
      // 1. there's no more to read OR
      // 2. we find the named element OR
      // 3. we find the named end element.
      while (rdr.Read() && (rdr.NodeType != XmlNodeType.Element || rdr.LocalName != name)
         && (rdr.NodeType != XmlNodeType.EndElement || rdr.LocalName != endname))
         ;

      // Return true, if we found the named element.
      return (rdr.NodeType == XmlNodeType.Element && rdr.LocalName == name);
   }

   private static void PrintSimpleNode(XmlReader rdr, string name)
   {
      AdvanceToNode(rdr, name);
      Console.WriteLine($"{rdr.LocalName}: {rdr.ReadElementContentAsString()}");
   }

   private static void PrintStatistics(XmlReader rdr)
   {
      Console.WriteLine("Statistics:");
      AdvanceToNode(rdr, QuadraticSieve.StatisticsNodeName);
      while (AdvanceToPossibleNode(rdr, QuadraticSieve.StatisticNodeName,
         QuadraticSieve.StatisticsNodeName))
      {
         AdvanceToNode(rdr, Statistic.NameNodeName);
         string name = rdr.ReadElementContentAsString();
         AdvanceToNode(rdr, Statistic.ValueNodeName);
         string value = rdr.ReadElementContentAsString();
         Console.WriteLine($"{name}: {value}");
      }
   }

   private static void PrintRelations(XmlReader rdr)
   {
      AdvanceToNode(rdr, Relations2P.TypeNodeName);
      string relationsType = rdr.ReadElementContentAsString();

      Console.WriteLine($"Relations Type: {relationsType}");
      int[] relationCount = new int[4];
      AdvanceToNode(rdr, Relations2P.RelationsNodeName);
      while(AdvanceToPossibleNode(rdr, "r", Relations2P.RelationsNodeName))
      {
         AdvanceToNode(rdr, "origin");
         string sOrigin = rdr.ReadElementContentAsString();
         RelationOrigin origin = (RelationOrigin)Enum.Parse(typeof(RelationOrigin), sOrigin);
         relationCount[(int)origin]++;
      }

      Console.WriteLine($"Full Relations: {relationCount.Sum()}");
      for (int j = 0; j < relationCount.Length; j ++)
         Console.WriteLine($"   {(RelationOrigin)j}: {relationCount[j]}");

      //
      // Partial Relations
      //
      for (int j = 0; j < relationCount.Length; j++)
         relationCount[j] = 0;
      AdvanceToNode(rdr, Relations2P.PartialRelationsNodeName);
      while (AdvanceToPossibleNode(rdr, "r", Relations2P.PartialRelationsNodeName))
      {
         if (relationsType == "Relations2P")
         {
            int index = 0;
            AdvanceToNode(rdr, TPRelation.PrimesNodeName);
            while (AdvanceToPossibleNode(rdr, TPRelation.PrimeNodeName, TPRelation.PrimesNodeName))
               index++;
            relationCount[index]++;
         }
         else
         {
            AdvanceToNode(rdr, TPRelation.OriginNodeName);
            string sOrigin = rdr.ReadElementContentAsString();
            RelationOrigin origin = (RelationOrigin)Enum.Parse(typeof(RelationOrigin), sOrigin);
            relationCount[(int)origin]++;
         }
      }

      Console.WriteLine($"Partial Relations: {relationCount.Sum()}");
      for (int j = 1; j < relationCount.Length; j++)
         Console.WriteLine($"   {(RelationOrigin)j}: {relationCount[j]}");
   }
}
