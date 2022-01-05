using System;
using System.Diagnostics;

namespace Friendly.Library
{
   public static class Assertions
   {
      [Conditional("DEBUG")]
      public static void True(bool expression)
      {
         if (!expression)
            throw new ApplicationException($"{nameof(expression)} must be true");
      }
   }
}
