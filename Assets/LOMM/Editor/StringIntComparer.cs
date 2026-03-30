using System;
using System.Collections.Generic;

public class StringIntComparer : IEqualityComparer<(string, int)>
{
    public bool Equals((string, int) x, (string, int) y)
    {
        return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
            x.Item2 == y.Item2;
    }

    public int GetHashCode((string, int) obj)
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
