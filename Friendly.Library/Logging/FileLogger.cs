#nullable enable

using System;
using System.IO;

namespace Friendly.Library.Logging
{
   public class FileLogger : ILog, IDisposable
   {
      private TextWriter? _tw;

      public FileLogger(string filename)
      {
         _tw = new StreamWriter(filename, true);
      }

      public void Write(string s)
      {
         if (_tw is null)
            throw new ObjectDisposedException(nameof(FileLogger));
         _tw.Write(s);
      }

      public void WriteLine(string s)
      {
         if (_tw is null)
            throw new ObjectDisposedException(nameof(FileLogger));
         _tw.WriteLine(s);
      }

      public void Flush()
      {
         if (_tw is null)
            throw new ObjectDisposedException(nameof(FileLogger));
         _tw.Flush();
      }

      protected virtual void Dispose(bool disposing)
      {
         _tw?.Flush();
         _tw?.Close();
         _tw = null;
      }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
   }
}

