using System;
namespace Friendly.Library.Utility
{
   /// <summary>
   /// An instance of this Exception class is thrown when aborting due to
   /// the user pressing Ctrl+C.
   /// </summary>
   public class AbortException : Exception
   {
      public AbortException()
      {
      }
   }
}

