using System;

namespace Friendly.Library.QuadraticSieve
{
   public interface INotifyProgress
   {
      event EventHandler<NotifyProgressEventArgs> Progress;
   }
}

