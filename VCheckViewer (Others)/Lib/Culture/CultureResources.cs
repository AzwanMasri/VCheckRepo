using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using VCheckViewer_Others.Log;

namespace VCheckViewer_Others.Lib.Culture
{
    public class CultureResources
    {
        private static bool bFoundInstalledCultures = false;

        private static List<CultureInfo> pSupportedCultures = new List<CultureInfo>();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// List of available cultures, enumerated at startup
        /// </summary>
        public static List<CultureInfo> SupportedCultures
        {
            get { return pSupportedCultures; }
        }

        static CultureResources()
        {
            if (!bFoundInstalledCultures)
            {
                //determine which cultures are available to this application
                //Debug.WriteLine("Get Installed cultures:");
                CultureInfo tCulture = new CultureInfo("");
                var dirList = Directory.GetDirectories(Application.StartupPath);
                var excludeList = new string[] { "Content", "LatoFont", "publish", "runtimes" };
                foreach (string dir in dirList)
                {
                    try
                    {
                        //see if this directory corresponds to a valid culture name
                        DirectoryInfo dirinfo = new DirectoryInfo(dir);
                        if (excludeList.Contains(dirinfo.Name)) { continue; }
                        tCulture = CultureInfo.GetCultureInfo(dirinfo.Name);

                        //determine if a resources dll exists in this directory that matches the executable name
                        if (dirinfo.GetFiles(Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".resources.dll").Length > 0)
                        {
                            pSupportedCultures.Add(tCulture);
                            Debug.WriteLine(string.Format(" Found Culture: {0} [{1}]", tCulture.DisplayName, tCulture.Name));
                        }
                    }
                    catch (CultureNotFoundException cex)
                    {
                        //App.log.Error("Culture Not Found Error >>> ", cex);
                    }
                    catch (ArgumentException ex) //ignore exceptions generated for any unrelated directories in the bin folder
                    {
                        App.log.Error("Culture Argument Error >>> ", ex);
                    }
                    catch (Exception cex)
                    {
                        App.log.Error("Culture Error >>> ", cex);
                    }
                }
                bFoundInstalledCultures = true;
            }
        }

        /// <summary>
        /// The Resources ObjectDataProvider uses this method to get an instance of the WPFLocalize.Properties.Resources class
        /// </summary>
        /// <returns></returns>
        public VCheckViewer_Others.Properties.Resources GetResourceInstance()
        {
            return new VCheckViewer_Others.Properties.Resources();
        }

        public static ObjectDataProvider m_provider;
        public static ObjectDataProvider ResourceProvider
        {
            get
            {
                if (m_provider == null)
                    m_provider = (ObjectDataProvider)App.Current.FindResource("Resources");
                return m_provider;
            }
        }

        /// <summary>
        /// Change the current culture used in the application.
        /// If the desired culture is available all localized elements are updated.
        /// </summary>
        /// <param name="culture">Culture to change to</param>
        public static void ChangeCulture(CultureInfo culture)
        {
            //remain on the current culture if the desired culture cannot be found
            // - otherwise it would revert to the default resources set, which may or may not be desired.
            if (pSupportedCultures.Contains(culture))
            {
                Properties.Resources.Culture = culture;
                ResourceProvider.Refresh();

                //SatelliteLibrary.Cultures.CultureResources.ChangeCulture(culture);
            }
            else
                Debug.WriteLine(string.Format("Culture [{0}] not available", culture));
        }
    }
}
