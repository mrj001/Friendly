using System;

namespace Friendly.Library.QuadraticSieve
{
   public class NotifyProgressEventArgs : EventArgs
   {
      private readonly string _message;

      public NotifyProgressEventArgs(string message)
      {
         _message = message;
      }

      public string Message { get => _message; }
   }
}

