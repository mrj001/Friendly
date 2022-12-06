using System;
using System.IO;
using System.Xml;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;
using Xunit;

namespace Test.Library.QuadraticSieve
{
   public class TestTPRelation
   {
      //--------------------------------------------------------------------
      public static TheoryData<long, long, long[], RelationOrigin, string> ctorXml_TestData
      {
         get
         {
            TheoryData<long, long, long[], RelationOrigin, string> rv = new();

            // Two Large Primes with origin specified
            rv.Add(1234, 12, new long[] { 65537, 94321 }, RelationOrigin.TwoLargePrimes,
               "<r><qofx>1234</qofx><x>12</x><exponentvector><capacity>64</capacity><bits>0000000000000000</bits></exponentvector><primes><prime>65537</prime><prime>94321</prime></primes><origin>TwoLargePrimes</origin></r>");

            // Two Large Primes with origin inferred.
            rv.Add(1234, 12, new long[] { 65537, 94321 }, RelationOrigin.TwoLargePrimes,
               "<r><qofx>1234</qofx><x>12</x><exponentvector><capacity>64</capacity><bits>0000000000000000</bits></exponentvector><primes><prime>65537</prime><prime>94321</prime></primes></r>");

            // One Large Prime with origin specified.
            rv.Add(12345, 23, new long[] { 65537 }, RelationOrigin.ThreeLargePrimes,
               "<r><qofx>12345</qofx><x>23</x><exponentvector><capacity>64</capacity><bits>0000000000000000</bits></exponentvector><primes><prime>65537</prime></primes><origin>ThreeLargePrimes</origin></r>");

            // One Large Prime with origin inferred.
            rv.Add(12345, 23, new long[] { 65537 }, RelationOrigin.OneLargePrime,
               "<r><qofx>12345</qofx><x>23</x><exponentvector><capacity>64</capacity><bits>0000000000000000</bits></exponentvector><primes><prime>65537</prime></primes></r>");

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctorXml_TestData))]
      public void CtorXml(long expectedQofX, long expectedX, long[] expectedPrimes,
         RelationOrigin expectedOrigin, string xml)
      {
         XmlDocument doc = new XmlDocument();
         using (StringReader sr = new StringReader(xml))
         using (XmlReader xmlr = XmlReader.Create(sr))
            doc.Load(xmlr);

         TPRelation actual = new(doc.FirstChild!);

         Assert.Equal(expectedQofX, actual.QOfX);
         Assert.Equal(expectedX, actual.X);
         Assert.Equal(expectedPrimes.Length, actual.Count);
         for (int j = 0; j < expectedPrimes.Length; j++)
            Assert.Equal(expectedPrimes[j], actual[j]);
         Assert.Equal(expectedOrigin, actual.Origin);
      }

      //--------------------------------------------------------------------
      public static TheoryData<long, long, long[], RelationOrigin> Serialize_TestData
      {
         get
         {
            TheoryData<long, long, long[], RelationOrigin> rv = new();

            rv.Add(1234, 12, new long[] { 65537, 94321 }, RelationOrigin.TwoLargePrimes);

            rv.Add(12345, 23, new long[] { 65537 }, RelationOrigin.ThreeLargePrimes);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(Serialize_TestData))]
      public void Serialize(long qofX, long x, long[] primes, RelationOrigin origin)
      {
         BigBitArray exponentVector = new BigBitArray(64);
         Random rnd = new Random(123);
         for (int j = 0; j < 6; j++)
            exponentVector.FlipBit(rnd.Next(0, (int)exponentVector.Capacity));
         TPRelation expected = new TPRelation(qofX, x, exponentVector, primes, origin);
         XmlDocument doc = new XmlDocument();
         XmlNode xmlNode = expected.Serialize(doc, "mytestnode");

         TPRelation actual = new(xmlNode);

         Assert.Equal(expected.QOfX, actual.QOfX);
         Assert.Equal(expected.X, actual.X);
         // Assert that Exponent Vectors are equal
         Assert.Equal(expected.ExponentVector.PopCount(), actual.ExponentVector.PopCount());
         BigBitArray tstBits = new BigBitArray(expected.ExponentVector);
         tstBits.Xor(actual.ExponentVector);
         Assert.Equal(0, tstBits.PopCount());

         // Assert that the set of primes are equal.
         Assert.Equal(expected.Count, actual.Count);
         for (int j = 0; j < expected.Count; j++)
            Assert.Equal(expected[j], actual[j]);

         Assert.Equal(expected.Origin, actual.Origin);
      }
   }
}

