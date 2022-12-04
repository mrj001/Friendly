using System.Xml;

namespace Friendly.Library.Utility
{
   public interface ISerialize
   {
      /// <summary>
      /// Serializes the implementing object to XML.
      /// </summary>
      /// <param name="document">The document to which the returned node can be added.</param>
      /// <param name="name">The name of the new node.</param>
      /// <returns>An XmlNode containing a representation of the object.  It is
      /// not added to document.</returns>
      XmlNode Serialize(XmlDocument document, string name);
   }
}