using System;
using System.Diagnostics;
using System.Reflection;

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

      [Conditional("DEBUG")]
      public static void True<T>(bool expression, string message) where T : Exception
      {
         if (!expression)
         {
            ConstructorInfo ctor = typeof(T).GetConstructor(new Type[] { typeof(string) });
            throw (Exception)ctor.Invoke(new object[] { message });
         }
      }
   }
}
