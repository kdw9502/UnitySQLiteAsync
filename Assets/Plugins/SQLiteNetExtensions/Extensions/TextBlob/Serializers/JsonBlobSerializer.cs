using System;
using LitJson;

namespace SQLiteNetExtensions.Extensions.TextBlob.Serializers
{
    public class JsonBlobSerializer : ITextBlobSerializer
    {
        public string Serialize(object element)
        {
            return LitJson.JsonMapper.ToJson(element);
        }

        public object Deserialize(string text, Type type)
        {
            return LitJson.JsonMapper.ToObject(text, type);
        }
    }
}