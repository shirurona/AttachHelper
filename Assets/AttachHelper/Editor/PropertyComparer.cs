using System.Collections.Generic;

namespace AttachHelper.Editor
{
    public class PropertyComparer : IEqualityComparer<UniquePropertyInfo>
    {
        public bool Equals(UniquePropertyInfo x, UniquePropertyInfo y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.GlobalObjectIdString.Equals(y.GlobalObjectIdString) && x.PropertyPath.Equals(y.PropertyPath);
        }

        public int GetHashCode(UniquePropertyInfo obj)
        {
            unchecked
            {
                return ((obj.GlobalObjectIdString != null ? obj.GlobalObjectIdString.GetHashCode() : 0) * 397) ^ (obj.PropertyPath != null ? obj.PropertyPath.GetHashCode() : 0);
            }
        }
    }
}