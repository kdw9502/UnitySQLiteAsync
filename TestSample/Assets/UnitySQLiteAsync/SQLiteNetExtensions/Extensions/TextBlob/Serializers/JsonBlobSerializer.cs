using System;
using UnityEngine;

namespace SQLiteNetExtensions.Extensions.TextBlob.Serializers
{
    public class JsonBlobSerializer : ITextBlobSerializer
    {
        public string Serialize(object element)
        {
            return JsonUtility.ToJson(element);
        }

        public object Deserialize(string text, Type type)
        {
            return JsonUtility.FromJson(text, type);
        }
    }
}