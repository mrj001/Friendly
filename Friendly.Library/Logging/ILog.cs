using System;

namespace Friendly.Library.Logging
{
   public interface ILog
   {
      void Write(string s);

      void WriteLine(string s);

      void Flush();
   }
}