using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public static class UIActionManager
{
    public static Action OnPreLoadLevel;
    public static Action OnPostLoadLevel;
    public static Action OnReset;
    public static Action OnPreClearLevel;
    public static Action<int> OnSelectObject;
    public static Action<Color, string> OnChangeLightColor;
    public static Action<float, string> OnChangeLightRadius;

    public static Action<string> OnSelectObjectIn3D;

    public static Action OnExport;
    public static Action OnExportSelectedObject;
    public static Action OnExportAll;
}