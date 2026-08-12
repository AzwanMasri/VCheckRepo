using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VCheckViewer_Others
{
    class AppTheme
    {
        public static void ChangeTheme(Uri themeURI)
        {
            ResourceDictionary theme = new ResourceDictionary()
            {
                Source = themeURI
            };

            App.Current.Resources.Clear();
            App.Current.Resources.MergedDictionaries.Add(theme);
        }
    }
}
