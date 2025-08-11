using UnityEditor;

namespace AttachHelper.Editor
{
    public class UniqueProperty : UniquePropertyInfo
    {
        public SerializedProperty SerializedProperty { get; }
        
        public UniqueProperty(string globalObjectIdString, SerializedProperty property) : base(globalObjectIdString, property.propertyPath)
        {
            SerializedProperty = property.Copy();
        }
    }
}