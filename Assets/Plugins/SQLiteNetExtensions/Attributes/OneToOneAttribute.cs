namespace SQLiteNetExtensions.Attributes
{
    public class OneToOneAttribute : RelationshipAttribute
    {
        public OneToOneAttribute(string foreignKey = null, string inverseProperty = null) 
            : base(foreignKey, null, inverseProperty)
        {
        }
    }
}