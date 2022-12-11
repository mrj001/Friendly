#nullable enable
using System;

namespace Friendly.Library.Utility
{
   /// <summary>
   /// A simple Name/Value pair for returning statistical information.
   /// </summary>
   public class Statistic
   {
      private readonly string _name;
      private readonly object _value;

      public Statistic(string name, object value)
      {
         _name = name;
         _value = value;
      }

      public string Name { get => _name; }

      public object Value { get => _value; }

      public override string ToString()
      {
         return $"{_name}: {_value}";
      }
   }
}

