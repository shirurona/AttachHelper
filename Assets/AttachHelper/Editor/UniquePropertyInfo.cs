namespace AttachHelper.Editor
{
    public class UniquePropertyInfo
    {
        public string GlobalObjectIdString { get; }
        public string PropertyPath { get; }
        
        public UniquePropertyInfo(string globalObjectIdString, string propertyPath)
        {
            GlobalObjectIdString = globalObjectIdString;
            PropertyPath = propertyPath;
        }
    }
}