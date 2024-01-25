using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class UIActionManager
{
    public static Action OnPreLoadLevel;
    public static Action OnPostLoadLevel;
    public static Action OnReset;
    public static Action OnPreClearLevel;
}