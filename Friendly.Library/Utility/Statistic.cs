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

      public Statistic(XmlNode node)
      {
         XmlNode? nameNode = node.FirstChild;
         SerializeHelper.ValidateNode(nameNode, NameNodeName);
         _name = nameNode!.InnerText;

         XmlNode? typeNode = nameNode.NextSibling;
         SerializeHelper.ValidateNode(typeNode, TypeNodeName);
         string typeName = typeNode!.InnerText;

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

         XmlNode? valueNode = typeNode.NextSibling;
         SerializeHelper.ValidateNode(valueNode, ValueNodeName);

         MethodInfo? method = valueType?.GetMethod("Parse", new Type[] { typeof(string) });
         _value = method?.Invoke(null, new object[] { valueNode!.InnerText }) ?? valueNode!.InnerText;
      }

      public string Name { get => _name; }

      public object Value { get => _value; }

      public override string ToString()
      {
         return $"{_name}: {_value}";
      }

      #region ISerialize
      public XmlNode Serialize(XmlDocument document, string name)
      {
         XmlNode rv = document.CreateElement(name);

         XmlNode nameNode = document.CreateElement(NameNodeName);
         nameNode.InnerText = _name;
         rv.AppendChild(nameNode);

         XmlNode typeNode = document.CreateElement(TypeNodeName);
         typeNode.InnerText = _value.GetType().ToString();
         rv.AppendChild(typeNode);

         XmlNode valueNode = document.CreateElement(ValueNodeName);
         valueNode.InnerText = _value.ToString()!;
         rv.AppendChild(valueNode);

         return rv;
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

