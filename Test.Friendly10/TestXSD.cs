using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace Test.Friendly10
{
   public class TestXSD
   {
      [Fact]
      public void Compiles()
      {
         XmlSchemaSet schemas = new XmlSchemaSet();

         Assembly assy = AppDomain.CurrentDomain.GetAssemblies().
            Where(a => !a.IsDynamic && Path.GetFileName(a.Location) == "Friendly10.dll").
                 First();

         using (Stream xsd = assy.GetManifestResourceStream("Friendly10.Assets.SavePoint.xsd"))
            schemas.Add(XmlSchema.Read(xsd, null));

         // If this schemais faulty, this will throw an XmlSchemaException and fail
         // the test.
         schemas.Compile();
      }

      public static TheoryData<bool, string> ValidationTestData
      {
         get
         {
            var rv = new TheoryData<bool, string>();

            // Single digit validates
            rv.Add(true,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>5</savepoint>");

            // Two digits validates
            rv.Add(true,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>50</savepoint>");

            // Three digits validates
            rv.Add(true,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>507</savepoint>");

            // Four digits with no comma does not validate
            rv.Add(false,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>3141</savepoint>");

            // Four digits with a comma does validates
            rv.Add(true,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>3,141</savepoint>");

            // 11 digits with commas does validates
            rv.Add(true,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>13,351,210,125</savepoint>");

            // only two digits between commas does not validate
            rv.Add(false,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>13,35,210,125</savepoint>");

            rv.Add(true,
               @"<?xml version='1.0' encoding='UTF-8' ?>
<savepoint>
387,206
</savepoint>
");

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ValidationTestData))]
      public void Validation(bool expected, string xml)
      {
         XmlDocument doc = new XmlDocument();

         Assembly assy = AppDomain.CurrentDomain.GetAssemblies().
   Where(a => !a.IsDynamic && Path.GetFileName(a.Location) == "Friendly10.dll").
        First();

         using (Stream xsd = assy.GetManifestResourceStream("Friendly10.Assets.SavePoint.xsd"))
            doc.Schemas.Add(XmlSchema.Read(xsd, null));

         doc.LoadXml(xml);
         bool actual;
         try
         {
            doc.Validate(null);
            actual = true;
         }
         catch (XmlSchemaValidationException)
         {
            actual = false;
         }

         Assert.Equal(expected, actual);
      }
   }
}
