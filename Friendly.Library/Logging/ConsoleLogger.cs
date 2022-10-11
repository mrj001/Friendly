using System;

namespace Friendly.Library.Logging
{
   public class ConsoleLogger : ILog
   {
      public ConsoleLogger()
      {
      }

      public void Write(string s)
      {
         Console.Write(s);
      }

      public void WriteLine(string s)
      {
         Console.WriteLine(s);
      }

      public void Flush()
      {
         // Nothing to do here.
      }
   }
}

