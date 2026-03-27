using System;
using System.Collections.Generic;

public class StringStringComparer : IEqualityComparer<(string, string)>
{
    public bool Equals((string, string) x, (string, string) y)
    {
        return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode((string, string) obj)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1);
            hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2);
            return hash;
        }
    }
}
