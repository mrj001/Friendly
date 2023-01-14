#nullable enable
using System;
using System.Reflection;
using System.Xml;

namespace Friendly.Library.Utility
{
   /// <summary>
   /// A simple Name/Value pair for returning statistical information.
   /// </summary>
   public class Statistic : ISerialize
   {
      private readonly string _name;
      private readonly object _value;

      public const string NameNodeName = "name";
      public const string TypeNodeName = "type";
      public const string ValueNodeName = "value";

      public Statistic(string name, object value)
      {
         _name = name;
         _value = value;
      }

      public Statistic(XmlReader rdr)
      {
         rdr.ReadStartElement();

         rdr.ReadStartElement(NameNodeName);
         _name = rdr.ReadContentAsString();
         rdr.ReadEndElement();

         rdr.ReadStartElement(TypeNodeName);
         string typeName = rdr.ReadContentAsString();
         rdr.ReadEndElement();

         Type? valueType = null;
         foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
         {
            Type? t = a.GetType(typeName);
            if (t is not null)
            {
               valueType = t;
               break;
            }
         }

         rdr.ReadStartElement(ValueNodeName);
         string value = rdr.ReadContentAsString();
         rdr.ReadEndElement();

         MethodInfo? method = valueType?.GetMethod("Parse", new Type[] { typeof(string) });
         _value = method?.Invoke(null, new object[] { value }) ?? value;

         rdr.ReadEndElement();
      }

      public string Name { get => _name; }

      public object Value { get => _value; }

      public override string ToString()
      {
         return $"{_name}: {_value}";
      }

      #region ISerialize
      public void Serialize(XmlWriter writer, string name)
      {
         writer.WriteStartElement(name);

         writer.WriteElementString(NameNodeName, _name);
         writer.WriteElementString(TypeNodeName, _value.GetType().ToString());
         writer.WriteElementString(ValueNodeName, _value.ToString());

         writer.WriteEndElement();
      }

      public void BeginSerialize()
      {
         // Empty
      }

      public void FinishSerialize(SerializationReason reason)
      {
         // Empty
      }
      #endregion
   }
}

