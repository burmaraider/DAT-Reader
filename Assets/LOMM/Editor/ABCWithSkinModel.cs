using System.Collections.Generic;

public class ABCWithSkinModel
{
    public ABCModel ABCModel { get; set; }
    public string AllSkinsPathsLowercase { get; set; }
    public int UniqueIndex { get; set; }
    public string UnityPathAndFilenameToPrefab { get; set; }

    public List<string> GetSkinList()
    {
        return AllSkinsPathsLowercase.SplitOnSemicolonAndLowercase();
    }

    public string GetNameSuffix()
    {
        if (UniqueIndex == 0)
        {
            return string.Empty;
        }

        return UniqueIndex.ToString();
    }
}
