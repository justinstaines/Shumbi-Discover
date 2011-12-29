using System.Windows;
using System;
using System.Windows.Threading;
using System.Threading;
using System.Reflection;
using System.Text;
using Obany.UI.Dialogs;
using System.Collections.Generic;

namespace ShumbiDiscover
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Fields
        private static Mutex _mutex;
        private static bool _newMutex;
        private static AboutData _aboutData;
        private static bool _runApp;
        #endregion

        #region Entry Point
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static void Main()
        {
            _runApp = true;

            Obany.Core.Logging.ExceptionLogged += new Obany.Core.Logging.ExceptionLoggedEventHandler(Logging_ExceptionLogged);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            if (!Obany.Core.FrameworkVersionDetection.IsInstalled(Obany.Core.FrameworkVersion.Fx35))
            {
                string cult = Thread.CurrentThread.CurrentUICulture.ToString();
                MessageBox.Show(Obany.Core.CultureHelper.GetStringForLanguage(ShumbiDiscover.Properties.AppResources.ResourceManager, "DOTNET35", cult),
                                Obany.Core.CultureHelper.GetStringForLanguage(Obany.UI.Properties.Resources.ResourceManager, "INFORMATION", cult),
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                _runApp = false;
            }
            else
            {
                if (!IsAlreadyRunning())
                {
                    _aboutData = new AboutData();
                    _aboutData.Contributors = "Martyn Janes\nJustin Staines";
                    _aboutData.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    _aboutData.VersionTag = "";
                    _aboutData.Copyright = "© 2009,2010 Shumbi Ltd";
                    _aboutData.Patents = "GB0914269.6, GB0914723.2";

#if BAMBOO
                    _aboutData.ProductName = "Bamboo Explore";
                    _aboutData.PoweredBy = "Shumbi Discover";
                    _aboutData.CompanyNamePlain = "Wacom";
                    _aboutData.Copyright += "\n© 2009,2010 Wacom Europe GmbH";
                    _aboutData.WebSite = "www.wacom.eu/bamboo/explore";
                    _aboutData.ProductIconResource = new Uri("/BambooExplore;component/Resources/Product.png", UriKind.Relative);
                    _aboutData.CompanyIconResource = new Uri("/BambooExplore;component/Resources/Company.png", UriKind.Relative);

                    Uri aboutButtonResource = new Uri("/BambooExplore;component/Resources/AboutButton.png", UriKind.Relative);
                    Uri themeUri = new Uri("/BambooExplore;component/Themes/Theme.xaml", UriKind.Relative);
                    bool sendLicensing = true;
                    bool testHardware = true;
                    string hardwareFailResource = "TABLETREQUIRED";
                    string vendorId = "056A";
                    List<string> checkProductsIds = new List<string>();
                    checkProductsIds.Add("00D0"); // Bamboo Touch (Maple T)
                    checkProductsIds.Add("00D1"); // Bamboo Touch (Maple S PT)
                    checkProductsIds.Add("00D2"); // Bamboo Touch (Maple Fun S PT)
                    checkProductsIds.Add("00D3"); // Bamboo Touch (Maple Fun M PT)
                    checkProductsIds.Add("00D4"); // Bamboo Touch (Maple One S P)
                    checkProductsIds.Add("00D5"); // Bamboo Touch (Maple One M P)
                    checkProductsIds.Add("00D6"); // Bamboo Touch (Maple PT S)
                    checkProductsIds.Add("00D7"); // Bamboo Touch (Maple Fun PT S)
                    checkProductsIds.Add("00D8"); // Bamboo Touch (Maple PT M)
                    checkProductsIds.Add("00D9"); // Bamboo Touch (Maple T S)
                    checkProductsIds.Add("00DA"); // Bamboo Touch (Maple Fun PT S (Special Edition))
                    checkProductsIds.Add("00DB"); // Bamboo Touch (Maple Fun PT M (Special Edition))
                    bool flicksAllowed = true;
                    string watermark = "";

#else
                    _aboutData.ProductName = "Shumbi Discover";
                    _aboutData.CompanyNamePlain = "Shumbi";
                    _aboutData.ProductSubName = "";
                    _aboutData.WebSite = "shumbi.com/shumbidiscover";
                    _aboutData.ProductIconResource = new Uri("/ShumbiDiscover;component/Resources/Product.png", UriKind.Relative);
                    _aboutData.CompanyIconResource = new Uri("/ShumbiDiscover;component/Resources/Company.png", UriKind.Relative);

                    Uri aboutButtonResource = new Uri("/ShumbiDiscover;component/Resources/AboutButton.png", UriKind.Relative);
                    Uri themeUri = new Uri("/ShumbiDiscover;component/Themes/Theme.xaml", UriKind.Relative);
                    bool sendLicensing = true;
                    bool testHardware = false;
                    string hardwareFailResource = "";
                    string vendorId = "";
                    List<string> checkProductsIds = new List<string>();
                    bool flicksAllowed = false;
                    string watermark = _aboutData.ProductName;
#endif

                    if (testHardware)
                    {
                        if (!string.IsNullOrEmpty(vendorId))
                        {
                            List<string> detectedProductIds = ShumbiDiscover.MainWindow.GetVendorProductIds(vendorId);

                            bool found = false;

                            if (checkProductsIds.Count == 0)
                            {
                                found = detectedProductIds.Count > 0;
                            }
                            else
                            {
                                for (int i = 0; i < detectedProductIds.Count && !found; i++)
                                {
                                    if (checkProductsIds.Contains(detectedProductIds[i]))
                                    {
                                        found = true;
                                    }
                                }
                            }

                            if (!found)
                            {
                                string cult = Thread.CurrentThread.CurrentUICulture.ToString();

                                _runApp = false;

                                MessageBox.Show(Obany.Core.CultureHelper.GetStringForLanguage(ShumbiDiscover.Properties.AppResources.ResourceManager, hardwareFailResource, cult),
                                                _aboutData.ProductName + " " + Obany.Core.CultureHelper.GetStringForLanguage(Obany.UI.Properties.Resources.ResourceManager, "INFORMATION", cult),
                                                MessageBoxButton.OK, MessageBoxImage.Exclamation);

                            }
                        }
                    }

                    if (_runApp)
                    {
                        SplashScreen splashScreen = new SplashScreen("Resources\\SplashScreen.png");
                        splashScreen.Show(true);
                        splashScreen.Close(TimeSpan.FromMilliseconds(3000));

                        App app = new App();
                        app.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
                        app.InitializeComponent();

                        ShumbiDiscover.MainWindow mainWindow = new MainWindow();
                        mainWindow.SetAboutData(_aboutData, aboutButtonResource, sendLicensing, vendorId, watermark);
                        mainWindow.SetFlicksAllowed(flicksAllowed);

                        app.Run(mainWindow);
                    }
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public App()
        {
        }
        #endregion

        #region Exception Handling
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Obany.Core.Logging.LogException("Unhandled Exception", e.ExceptionObject as Exception, null, null, false);
        }

        static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            Obany.Core.Logging.LogException("Dispatcher Unhandled Exception", e.Exception, null, null, false);
        }

        static void Logging_ExceptionLogged(Obany.Core.LogEntry logEntry, bool isWarning)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
            else
#endif
            {
                logEntry.ApplicationName = _aboutData.ProductName;
                logEntry.ApplicationVersion = _aboutData.VersionStringFixed;

                _runApp = false;

                try
                {
                    Obany.Communications.CommunicationsManager.Put(new Uri("http://support.shumbi.com/bugreport/?product=" + _aboutData.ProductName + "&version=" + _aboutData.VersionStringFixed), null, "text/xml", Obany.Language.Xml.XmlHelper.BinarySerialize(logEntry));

                    if (!isWarning)
                    {
                        if (Application.Current != null)
                        {
                            if (Application.Current.Dispatcher != null)
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                                    new ThreadStart(
                                        delegate
                                        {
                                            Application.Current.Shutdown();
                                        }
                                    )
                                );
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }
        #endregion

        #region Detection Methods
        private static bool IsAlreadyRunning()
        {
            if (_mutex == null)
            {
                string strLoc = Assembly.GetExecutingAssembly().Location;
                string sExeName = System.IO.Path.GetFileName(strLoc);

                try
                {
                    _mutex = new Mutex(true, "Global\\" + sExeName, out _newMutex);
                    if (_newMutex)
                    {
                        _mutex.ReleaseMutex();
                    }
                }
                catch { }
            }

            return (!_newMutex);
        }

        #endregion
    }
}
