#nullable enable
using System;
using System.IO;
using System.Text;
using System.Xml;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;
using Xunit;

namespace Test.Library.QuadraticSieve
{
   public class TestRelation
   {
      //--------------------------------------------------------------------
      public static TheoryData<long, long, RelationOrigin, string> ctor3_TestData
      {
         get
         {
            var rv = new TheoryData<long, long, RelationOrigin, string>();

            rv.Add(1234, 12, RelationOrigin.OneLargePrime,
               "<r><qofx>1234</qofx><x>12</x><exponentvector><capacity>64</capacity><bits>0000000000000000</bits></exponentvector><origin>OneLargePrime</origin></r>");

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctor3_TestData))]
      public void ctor3(long expectedQ, long expectedX, RelationOrigin expectedOrigin, string xml)
      {
         Relation actual;
         using (StringReader sr = new StringReader(xml))
         using (XmlReader rdr = XmlReader.Create(sr))
         {
            rdr.Read();
            actual = new Relation(rdr);
         }

         Assert.Equal(expectedQ, actual.QOfX);
         Assert.Equal(expectedX, actual.X);
         Assert.Equal(expectedOrigin, actual.Origin);
      }

      //--------------------------------------------------------------------
      public static TheoryData<long, long, RelationOrigin> SerializeTestData
      {
         get
         {
            var rv = new TheoryData<long, long, RelationOrigin>();

            rv.Add(123456, 32, RelationOrigin.TwoLargePrimes);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SerializeTestData))]
      public void Serialize(long qofx, long x, RelationOrigin origin)
      {
         XmlDocument doc = new XmlDocument();
         Relation expected = new Relation(qofx, x, new BigBitArray(64), origin);

         string nodeName = "mynode";
         XmlNode actualNode = expected.Serialize(doc, nodeName);
         doc.AppendChild(actualNode);

         Assert.Equal(nodeName, actualNode.LocalName);

         Relation actual;
         StringBuilder sb = new StringBuilder();
         using (StringWriter sw = new StringWriter(sb))
            doc.Save(sw);

         using (StringReader sr = new StringReader(sb.ToString()))
         using (XmlReader rdr = XmlReader.Create(sr))
         {
            rdr.Read();
            actual = new Relation(rdr);
         }

         Assert.Equal(expected.QOfX, actual.QOfX);
         Assert.Equal(expected.X, actual.X);
         Assert.Equal(expected.Origin, actual.Origin);
      }
   }
}

