using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DictionaryEquality : IEqualityComparer<Dictionary<string, string>>
{

    public bool Equals(Dictionary<string, string> x, Dictionary<string, string> y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;
        if (x.Count != y.Count)
            return false;

        foreach (var kvp in x)
        {
            if (!y.TryGetValue(kvp.Key, out string value) || !kvp.Value.Equals(value))
                return false;
        }

        return true;
    }

    public int GetHashCode(Dictionary<string, string> obj)
    {
        int hash = 17;
        foreach (var kvp in obj)
        {
            hash = hash * 23 + (kvp.Key.GetHashCode() ^ kvp.Value.GetHashCode());
        }
        return hash;
    }

}
