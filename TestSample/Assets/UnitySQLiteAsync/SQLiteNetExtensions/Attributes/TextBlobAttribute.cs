using System;

namespace SQLiteNetExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextBlobAttribute : RelationshipAttribute
    {
        public TextBlobAttribute(string textProperty) : base(null, null, null)
        {
            TextProperty = textProperty;
        }

        public string TextProperty { get; private set; }

        // No cascade operations allowed on TextBlob properties
        public override CascadeOperation CascadeOperations { get { return CascadeOperation.None; } }
    }
}
