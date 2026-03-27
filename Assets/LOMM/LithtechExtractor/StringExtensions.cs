using System.Collections.Generic;
using UnityEngine;

public static class StringExtensions
{
    public static string ConvertFolderSeperators(this string path)
    {
        return path.Replace("\\\\", "\\").Replace("/", "\\");
    }

    public static void LogElapsedTime(this System.Diagnostics.Stopwatch stopwatch, string message, int tabsToInsert = 0, bool restart = true)
    {
        Debug.Log(GetElapsedTime(stopwatch, message, tabsToInsert, restart));
    }

    public static string GetElapsedTime(this System.Diagnostics.Stopwatch stopwatch, string message, int tabsToInsert = 0, bool restart = true)
    {
        string elapsed = string.Format("{0,2:00}:{1,2:00}.{2,-3:000}", stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds, stopwatch.Elapsed.Milliseconds);
        string prefix = new string('\t', tabsToInsert);

        if (restart)
        {
            stopwatch.Restart();
        }

        return $"{prefix}{elapsed} {message}";
    }

    public static List<string> SplitOnSemicolonAndLowercase(this string skins)
    {
        var list = new List<string>();

        if (skins != null)
        {
            var splitSkins = skins.Split(";");
            foreach (var splitItem in splitSkins)
            {
                var item = (splitItem ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(item))
                {
                    list.Add(item.ConvertFolderSeperators().ToLower());
                }
            }
        }

        return list;
    }
}