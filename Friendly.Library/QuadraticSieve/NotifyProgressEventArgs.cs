using System;

namespace Friendly.Library.QuadraticSieve
{
   public class NotifyProgressEventArgs : EventArgs
   {
      private readonly string _message;
      private readonly TimeSpan _factorTime;

      public NotifyProgressEventArgs(string message, TimeSpan factorTime)
      {
         _message = message;
         _factorTime = factorTime;
      }

      public string Message { get => _message; }

      public TimeSpan Time { get => _factorTime; }

      public override string ToString()
      {
         return $"{_factorTime:d\\:hh\\:mm\\:ss\\.ffff}: {_message}";
      }
   }
}

