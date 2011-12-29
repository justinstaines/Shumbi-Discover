#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Windows;
using System.Windows.Input;
using Obany.UI.Dialogs;
using System;
using System.Collections.Generic;
using Obany.Hid.Tablet;
using System.Windows.Interop;
using System.Diagnostics;
using Obany.Core;

namespace ShumbiDiscover
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private AboutData _aboutData;
        private Uri _aboutButtonResource;
        private bool _sendLicensing;
        private string _vendorId;
        private Update _update;
        private int _updateCount;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Core.CoreConfigurationClient.RegisterLanguages();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the about data
        /// </summary>
        /// <param name="aboutData">The about data</param>
        /// <param name="aboutButtonResource">The about button resource</param>
        /// <param name="sendLicensing">Send licensing information</param>
        /// <param name="vendorId">The vendor id for hardware</param>
        /// <param name="watermark">Watermark for annotation</param>
        public void SetAboutData(AboutData aboutData, Uri aboutButtonResource, bool sendLicensing, string vendorId, string watermark)
        {
            _aboutData = aboutData;
            _aboutButtonResource = aboutButtonResource;
            _sendLicensing = sendLicensing;
            _vendorId = vendorId;

            this.Title = _aboutData.ProductName;
            coreInterface.SetBranding(_aboutData, _aboutButtonResource, watermark);
        }

        /// <summary>
        /// Set the flicks allowed flag
        /// </summary>
        /// <param name="allowed"></param>
        public void SetFlicksAllowed(bool allowed)
        {
            coreInterface.SetFlicksAllowed(allowed);
        }
        #endregion

        #region Application Settings
        private void LoadSettings()
        {
            Obany.Preferences.Registry.PreferencesProviderConfiguration preferencesConfiguration = new Obany.Preferences.Registry.PreferencesProviderConfiguration();
            preferencesConfiguration.RegistryKey = Microsoft.Win32.Registry.CurrentUser;
            preferencesConfiguration.Root = @"Software\" + _aboutData.CompanyNamePlain + @"\" + _aboutData.ProductName + @"\";

            Obany.Preferences.Model.IPreferencesProvider preferencesProvider = new Obany.Preferences.Registry.PreferencesProvider(preferencesConfiguration);

            double defWidth = (System.Windows.SystemParameters.PrimaryScreenWidth - 20);
            double defHeight = (System.Windows.SystemParameters.PrimaryScreenHeight - 50);
            double defLeft = 10;
            double defTop = 10;

            bool maxim = preferencesProvider.GetBool("IsMaximized", false);
            this.Left = preferencesProvider.GetDouble("WindowLeft", defLeft);
            this.Top = preferencesProvider.GetDouble("WindowTop", defTop);
            this.Width = preferencesProvider.GetDouble("WindowWidth", defWidth);
            this.Height = preferencesProvider.GetDouble("WindowHeight", defHeight);

            this.WindowState = maxim ? WindowState.Maximized : WindowState.Normal;
            this.ResizeMode = maxim ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;

            buttonMaximize.Visibility = maxim ? Visibility.Collapsed : Visibility.Visible;
            buttonRestore.Visibility = maxim ? Visibility.Visible : Visibility.Collapsed;

            coreInterface.LoadSettings(preferencesProvider);

            if (_sendLicensing)
            {
                bool isRegistered = preferencesProvider.GetBool("IsRegistered", false);

                if (!isRegistered)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
                    {
                        License license = new License();
                        license.ApplicationName = _aboutData.ProductName;
                        license.ApplicationVersion = _aboutData.VersionStringFixed;
                        license.OS = Environment.OSVersion.Platform.ToString();
                        license.OSVersion = Environment.OSVersion.Version.ToString();
                        license.ServicePack = Environment.OSVersion.ServicePack;
                        license.DateTimeUtc = DateTime.Now.ToUniversalTime();
                        license.ProcessorId = Obany.Core.Logging.GetManagementInfo("Win32_Processor", "ProcessorId");
                        license.BiosSerial = Obany.Core.Logging.GetManagementInfo("Win32_BIOS", "SerialNumber");
                        if (!string.IsNullOrEmpty(_vendorId))
                        {
                            license.OemHardwareId = string.Join(",", MainWindow.GetVendorProductIds(_vendorId).ToArray());
                        }

                        Obany.Communications.CommunicationsResult res = Obany.Communications.CommunicationsManager.Put(new Uri("http://support.shumbi.com/license/?product=" + _aboutData.ProductName + "&version=" + _aboutData.VersionStringFixed), null, "text/xml", Obany.Language.Xml.XmlHelper.BinarySerialize(license));

                        if (res.Status == Obany.Core.OperationStatus.Success)
                        {
                            if (res.BinaryResponse != null)
                            {
                                string resString = System.Text.Encoding.UTF8.GetString(res.BinaryResponse);

                                if (!string.IsNullOrEmpty(resString))
                                {
                                    if (resString.Trim() == "OK")
                                    {
                                        preferencesProvider.SetBool("IsRegistered", true);
                                    }
                                }                            
                            }
                        }
                    });
                }
            }

            _updateCount = preferencesProvider.GetInt32("UpdateCount", 0);
        }

        private void SaveSettings()
        {
            Obany.Preferences.Registry.PreferencesProviderConfiguration preferencesConfiguration = new Obany.Preferences.Registry.PreferencesProviderConfiguration();
            preferencesConfiguration.RegistryKey = Microsoft.Win32.Registry.CurrentUser;
            preferencesConfiguration.Root = @"Software\" + _aboutData.CompanyNamePlain + @"\" + _aboutData.ProductName + @"\";

            Obany.Preferences.Model.IPreferencesProvider preferencesProvider = new Obany.Preferences.Registry.PreferencesProvider(preferencesConfiguration);

            preferencesProvider.SetBool("IsMaximized", this.WindowState == WindowState.Maximized ? true : false);
            preferencesProvider.SetDouble("WindowLeft", this.Left);
            preferencesProvider.SetDouble("WindowTop", this.Top);
            preferencesProvider.SetDouble("WindowWidth", this.Width);
            preferencesProvider.SetDouble("WindowHeight", this.Height);

            preferencesProvider.SetInt32("UpdateCount", _updateCount + 1);

            coreInterface.SaveSettings(preferencesProvider);
        }
        #endregion

        #region Window Overrides
        /// <summary>
        /// The source window has been initialized
        /// </summary>
        /// <param name="e">The event args</param>
        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            WindowInteropHelper winHelper = new WindowInteropHelper(this);
            HwndSource hwnd = HwndSource.FromHwnd(winHelper.Handle);
            hwnd.AddHook(new HwndSourceHook(this.MessageProc));
                
            LoadSettings();

            coreInterface.Initialise();

            CheckForUpdate();
        }

        /// <summary>
        /// The window is closing
        /// </summary>
        /// <param name="e">The event args</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            coreInterface.Closedown();

            SaveSettings();
        }
        #endregion

        #region Window Caption Events
        private void thumbWindowDrag_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.Left += e.HorizontalChange;
            this.Top += e.VerticalChange;
        }

        private void thumbWindowDrag_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ToggleWindowState();
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;

        }

        private void buttonMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void ToggleWindowState()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;
            }
            buttonMaximize.Visibility = this.WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
            buttonRestore.Visibility = this.WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion

        #region Hardware Detection
        /// <summary>
        /// Get a list of the product ids for the vendor
        /// </summary>
        /// <param name="vendorId">The vendor id</param>
        /// <returns>List of devices</returns>
        public static List<string> GetVendorProductIds(string vendorId)
        {
            List<string> productIds = new List<string>();

            try
            {
                System.Management.ManagementClass mc = new System.Management.ManagementClass("Win32_USBControllerDevice");

                if(mc != null)
                {
                    System.Management.ManagementObjectCollection moc = mc.GetInstances();

                    foreach (System.Management.ManagementObject mo in moc)
                    {
                        try
                        {
                            string p = mo.Path.ToString();
                            int idx = p.IndexOf("Win32_PnPEntity.DeviceID=\\\"USB\\\\\\\\VID_" + vendorId);

                            if (idx > 0)
                            {
                                string pid = p.Substring(idx);
                                string[] parts = pid.Split('&');

                                foreach (string part in parts)
                                {
                                    string part2 = part;

                                    int idx2 = part2.IndexOf("\\");
                                    if (idx2 > 0)
                                    {
                                        part2 = part2.Substring(0, idx2);
                                    }

                                    if (part2.StartsWith("PID_"))
                                    {
                                        part2 = part2.Substring(4);
                                        if (!productIds.Contains(part2))
                                        {
                                            productIds.Add(part2);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {
            }


            return (productIds);
        }
        #endregion

        #region MessageProc Method
        /// <summary>
        /// Handle window messages
        /// </summary>
        /// <param name="hwnd">The window handle</param>
        /// <param name="nMsgID">The message</param>
        /// <param name="wParam">The wParam</param>
        /// <param name="lParam">The lParam</param>
        /// <param name="bHandled">Was the message handled</param>
        /// <returns>The message</returns>
        private IntPtr MessageProc(IntPtr hwnd, int nMsgID, IntPtr wParam, IntPtr lParam, ref bool bHandled)
        {
            if (nMsgID == 0x0024) // WM_GETMINMAXINFO
            {
                Obany.UI.Interop.MINMAXINFO mmi = (Obany.UI.Interop.MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(Obany.UI.Interop.MINMAXINFO));

                // Adjust the maximized size and position to fit the work area of the correct monitor
                int MONITOR_DEFAULTTONEAREST = 0x00000002;
                System.IntPtr monitor = Obany.UI.Interop.User32.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != System.IntPtr.Zero)
                {

                    Obany.UI.Interop.MONITORINFO monitorInfo = new Obany.UI.Interop.MONITORINFO();
                    Obany.UI.Interop.User32.GetMonitorInfo(monitor, monitorInfo);
                    Obany.UI.Interop.RECT rcWorkArea = monitorInfo.rcWork;
                    Obany.UI.Interop.RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                    mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                    mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                    mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                }

                System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);

                bHandled = true;
            }
            else
            {
                // Only allow tablet gestures through to main window if a dialog is not showing
                if (gridMain.IsEnabled)
                {
                    bHandled = TabletManager.ProcessMessage(nMsgID, lParam, wParam);
                }
            }

            return IntPtr.Zero;
        }
        #endregion

        #region Updates
        private void CheckForUpdate()
        {
            if (_updateCount % 5 == 0)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    try
                    {
                        Obany.Communications.CommunicationsResult res = Obany.Communications.CommunicationsManager.Get(new Uri("http://support.shumbi.com/version/?product=" + _aboutData.ProductName + "&version=" + _aboutData.VersionStringFixed + "&updatecount=" + _updateCount), null);

                        if (res.Status == Obany.Core.OperationStatus.Success)
                        {
                            if (res.BinaryResponse != null)
                            {
                                _update = Obany.Language.Xml.XmlHelper.BinaryDeserialize<Update>(res.BinaryResponse);

                                if (_update != null)
                                {
                                    DateTime currentBuildDate;
                                    DateTime newBuildDate;

                                    if (CalculateBuildDate(_aboutData.VersionStringFixed, out currentBuildDate))
                                    {
                                        if (CalculateBuildDate(_update.Version, out newBuildDate))
                                        {
                                            if (newBuildDate > currentBuildDate)
                                            {
                                                Action a = delegate()
                                                {
                                                    textUpdateAvailable.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "UPDATEAVAILABLE");
                                                    rowDefinitionUpdateAvailable.Height = new GridLength(40);
                                                    borderUpdateAvailable.Visibility = Visibility.Visible;
                                                };

                                                Dispatcher.Invoke(a);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                });
            }
        }

        private bool CalculateBuildDate(string version, out DateTime buildDate)
        {
            bool successful = false;
            buildDate = new DateTime(1900, 1, 1, 0, 0, 0);

            string[] parts = version.Split('.');

            if (parts.Length == 4)
            {
                string yearString = "";
                string monthString = "";
                string dayString = "";
                string hourString = "";
                string minuteString = "";

                if (parts[1].Length == 4)
                {
                    yearString = parts[1].Substring(2, 2);

                    if (parts[2].Length == 4)
                    {
                        monthString = parts[2].Substring(0, 2);
                        dayString = parts[2].Substring(2, 2);

                        if (parts[3].Length == 4)
                        {
                            hourString = parts[3].Substring(0, 2);
                            minuteString = parts[3].Substring(2, 2);

                            int year;
                            int month;
                            int day;
                            int hour;
                            int minute;

                            if (int.TryParse(yearString, out year))
                            {
                                if (int.TryParse(monthString, out month))
                                {
                                    if (int.TryParse(dayString, out day))
                                    {
                                        if (int.TryParse(hourString, out hour))
                                        {
                                            if (int.TryParse(minuteString, out minute))
                                            {
                                                try
                                                {
                                                    buildDate = new DateTime(year, month, day, hour, minute, 0);
                                                    successful = true;
                                                }
                                                catch
                                                {
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (successful);
        }

        private void textUpdateAvailable_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_update != null)
                {
                    if (!string.IsNullOrEmpty(_update.ProductName) && !string.IsNullOrEmpty(_update.Version))
                    {
                        Process.Start("http://support.shumbi.com/install/?product=" + _update.ProductName + "&version=" + _update.Version);
                    }
                }
            }
            catch
            {
            }
        }
        #endregion

    }
}
