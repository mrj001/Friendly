using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Schema;
using Xunit;

namespace Test.Library.QuadraticSieve
{
   public class TestXSD
   {
      [Fact]
      public void Compiles()
      {
         XmlSchemaSet schemas = new XmlSchemaSet();

         Assembly assy = AppDomain.CurrentDomain.GetAssemblies().
            Where(a => !a.IsDynamic && Path.GetFileName(a.Location) == "Friendly.Library.dll").
                 First();

         using (Stream xsd = assy.GetManifestResourceStream("Friendly.Library.Assets.QuadraticSieve.SaveQuadraticSieve.xsd")!)
            schemas.Add(XmlSchema.Read(xsd, null)!);

         // If this schema is faulty, this will throw an XmlSchemaException and fail
         // the test.
         schemas.Compile();
      }
   }
}

