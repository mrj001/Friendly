using System.Xml;

namespace Friendly.Library.Utility
{
   public enum SerializationReason
   {
      /// <summary>
      /// Specifies that the process is shutting down.
      /// </summary>
      Shutdown,

      /// <summary>
      /// Specifies that state is being saved and the process should resume
      /// factoring when serialization is completed.
      /// </summary>
      SaveState
   }

   public interface ISerialize
   {
      /// <summary>
      /// Serializes the implementing object to XML.
      /// </summary>
      /// <param name="writer">The XMLWriter to which the object is to write itself.</param>
      /// <param name="name">The name of the top-level node representing the object.</param>
      void Serialize(XmlWriter writer, string name);

      /// <summary>
      /// Called to begin the serialization process.
      /// </summary>
      /// <remarks>
      /// Objects must pause or wind down any threaded work.
      /// </remarks>
      void BeginSerialize();

      /// <summary>
      /// Called when the serialization is finished.
      /// </summary>
      /// <param name="reason">Specifies the action to take now that serialization
      /// is completed.</param>
      /// <remarks>
      /// If factoring is continuing, objects should resume their threaded work.
      /// </remarks>
      void FinishSerialize(SerializationReason reason);
   }
}