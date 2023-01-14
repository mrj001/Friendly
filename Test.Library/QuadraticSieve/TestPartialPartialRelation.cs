#nullable enable
using System;
using Friendly.Library.QuadraticSieve;
using System.IO;
using System.Xml;
using Xunit;
using Friendly.Library;
using System.Text;

namespace Test.Library.QuadraticSieve
{
   public class TestPartialPartialRelation
   {
      //--------------------------------------------------------------------
      public static TheoryData<long, long, long, long, string> ctor3_TestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long, string>();

            rv.Add(1234, 12, 65537, 94321, 
               "<r><qofx>1234</qofx><x>12</x><exponentvector><capacity>64</capacity><bits>0000000000000000</bits></exponentvector><primes><prime>65537</prime><prime>94321</prime></primes></r>");

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctor3_TestData))]
      public void ctor3(long expectedQ, long expectedX, long expectedPrime1,
         long expectedPrime2, string xml)
      {
         PartialPartialRelation actual;
         using (StringReader sr = new StringReader(xml))
         using (XmlReader xmlr = XmlReader.Create(sr))
         {
            xmlr.Read();
            actual = new PartialPartialRelation(xmlr);
         }

         Assert.Equal(expectedQ, actual.QOfX);
         Assert.Equal(expectedX, actual.X);
         Assert.Equal(expectedPrime1, actual.Prime1);
         Assert.Equal(expectedPrime2, actual.Prime2);
      }

      //--------------------------------------------------------------------
      public static TheoryData<long, long, long, long> SerializeTestData
      {
         get
         {
            var rv = new TheoryData<long, long, long, long>();

            rv.Add(1234, 21, 65537, 94321);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(SerializeTestData))]
      public void Serialize(long qofX, long x, long prime1, long prime2)
      {
         BigBitArray expVector = new BigBitArray(64);
         PartialPartialRelation expected = new(qofX, x, expVector, prime1, prime2);
         XmlDocument doc = new XmlDocument();
         XmlNode node = expected.Serialize(doc, "testing");
         doc.AppendChild(node);
         StringBuilder sb = new StringBuilder();
         using (StringWriter sw = new StringWriter(sb))
            doc.Save(sw);

         PartialPartialRelation actual;
         using (StringReader sr = new StringReader(sb.ToString()))
         using (XmlReader rdr = XmlReader.Create(sr))
         {
            rdr.Read();
            actual = new PartialPartialRelation(rdr);
         }

         Assert.Equal(expected.QOfX, actual.QOfX);
         Assert.Equal(expected.X, actual.X);
         Assert.Equal(expected.Prime1, actual.Prime1);
         Assert.Equal(expected.Prime2, actual.Prime2);
      }
   }
}

