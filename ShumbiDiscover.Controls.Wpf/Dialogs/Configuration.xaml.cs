#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Obany.Core;
using Obany.UI;
using ShumbiDiscover.Data;
using System.Diagnostics;
using Obany.UI.Controls;

namespace ShumbiDiscover.Controls.Dialogs
{
    /// <summary>
    /// Control for configuration dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class Configuration : UserControl, ILocalisable
    {
        #region Delegates
        /// <summary>
        /// Event handler for when a url open is required
        /// </summary>
        /// <param name="uri">The url to open</param>
        public delegate void OpenUrlRequestedEventHandler(Uri uri);
        
        /// <summary>
        /// Delegate definition for when a provider configure button is clicked
        /// </summary>
        /// <param name="provider">The name of the provider</param>
        public delegate void ConfigureProviderDelegate(string provider);

        /// <summary>
        /// Delegate definition for when a provider configure delete button is clicked
        /// </summary>
        /// <param name="provider">The name of the provider</param>
        public delegate void ConfigureProviderDeleteDelegate(string provider);

        /// <summary>
        /// Delegate definition for when the thumbnail cache is cleared
        /// </summary>
        public delegate void ThumbnailCacheClearDelegate();
        #endregion

        #region Fields
        private bool _initialising;
        private ObservableCollection<ApiCredential> _apiCredentials;
        private string _thumbnailCacheLocation;
#if SILVERLIGHT
        private string _authToken;
        private string _applicationUri;
        private Uri _licenseUri;
#endif
        #endregion

        #region Events
#if SILVERLIGHT
        /// <summary>
        /// Event call when a url open is requested
        /// </summary>
        public event OpenUrlRequestedEventHandler OpenUrlRequested;
#endif
        /// <summary>
        /// Event fired when a configure button is clicked
        /// </summary>
        public event ConfigureProviderDelegate ConfigureProvider;

        /// <summary>
        /// Event fired when a configure delete button is clicked
        /// </summary>
        public event ConfigureProviderDeleteDelegate ConfigureDeleteProvider;

        /// <summary>
        /// Event fired when the thumbnail cache is cleared
        /// </summary>
        public event ThumbnailCacheClearDelegate ThumbnailCacheClear;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Configuration()
        {
            InitializeComponent();

            CultureHelper.CultureChanged += new CultureHelper.CultureChangedEventHandler(CultureHelper_CultureChanged);

#if SILVERLIGHT
            buttonAccount.Visibility = Visibility.Visible;
            buttonProxy.Visibility = Visibility.Collapsed;
            buttonProxy.IsEnabled = false;

            labelThumbnails.Visibility = Visibility.Collapsed;
            labelThumbnailsCacheSize.Visibility = Visibility.Collapsed;
            panelThumbnailCacheSize.Visibility = Visibility.Collapsed;
            labelThumbnailsCacheSpaceUsed.Visibility = Visibility.Collapsed;
            textThumbnailCacheSpaceUsed.Visibility = Visibility.Collapsed;
            labelThumbnailCacheLocation.Visibility = Visibility.Collapsed;
            textThumbnailCacheLocation.Visibility = Visibility.Collapsed;
            buttonThumbnailCacheOpen.Visibility = Visibility.Collapsed;
            buttonThumbnailCacheEmpty.Visibility = Visibility.Collapsed;
#else
            buttonAccount.Visibility = Visibility.Collapsed;
            buttonAccount.IsEnabled = false;
            contentControlAccount.IsEnabled = false;
#endif

            buttonLicenseInformation.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set the languages
        /// </summary>
        public List<KeyValuePair<string, string>> Languages
        {
            set
            {
                foreach (KeyValuePair<string, string> language in value)
                {
                    TextBlock tb = new TextBlock();
                    tb.Tag = language.Value;
                    tb.Text = language.Key;
                    listLanguage.Items.Add(tb);
                }
            }
        }

        /// <summary>
        /// Get or set the language
        /// </summary>
        public string Culture
        {
            get
            {
                string culture = "en-US";

                if (listLanguage.SelectedItem != null)
                {
                    TextBlock tb = listLanguage.SelectedItem as TextBlock;

                    culture = (string)tb.Tag;
                }
                return (culture);
            }
            set
            {
                Action a = delegate()
                {
                    _initialising = true;

                    bool found = false;
                    for (int i = 0; i < listLanguage.Items.Count && !found; i++)
                    {
                        TextBlock tb = listLanguage.Items[i] as TextBlock;
                        if ((string)tb.Tag == value)
                        {
                            found = true;
                            listLanguage.SelectedIndex = i;
                        }
                    }

                    _initialising = false;
                };

                Dispatcher.BeginInvoke(a);
            }
        }

        /// <summary>
        /// Set the api credentials
        /// </summary>
        public ObservableCollection<ApiCredential> ApiCredentials
        {
            set
            {
                _apiCredentials = value;
                _apiCredentials.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_apiCredentials_CollectionChanged);
            }
        }

        /// <summary>
        /// Set or get the history item count
        /// </summary>
        public int HistoryItemCount
        {
            set
            {
                sliderHistoryToKeep.Value = value;
            }
            get
            {
                return ((int)Math.Round(sliderHistoryToKeep.Value, 0));
            }
        }

        /// <summary>
        /// Set the history space used
        /// </summary>
        public long HistorySpaceUsed
        {
            set
            {
                textHistorySpaceUsed.Text = CalculateSpaceUsedText(value);
            }
        }

        /// <summary>
        /// Set or get the thumbnail location
        /// </summary>
        public string ThumbnailCacheLocation
        {
            set
            {
                _thumbnailCacheLocation = value;
                textThumbnailCacheLocation.Text = value;
            }
            get
            {
                return (_thumbnailCacheLocation);
            }
        }

        /// <summary>
        /// Set or get the thumbnail size
        /// </summary>
        public int ThumbnailCacheSize
        {
            set
            {
                sliderThumbnailCacheSize.Value = value;
            }
            get
            {
                return ((int)Math.Round(sliderThumbnailCacheSize.Value, 0));
            }
        }

        /// <summary>
        /// Set or get the proxy mode
        /// </summary>
        public int ProxyMode
        {
            set
            {
                SetProxyMode(value);
            }
            get
            {
                return (GetProxyMode());
            }
        }

        /// <summary>
        /// Set or get the proxy address
        /// </summary>
        public string ProxyAddress
        {
            set
            {
                textProxyAddress.Text = value;
            }
            get
            {
                return (textProxyAddress.Text);
            }
        }

        /// <summary>
        /// Set or get the proxy port
        /// </summary>
        public int ProxyPort
        {
            set
            {
                textProxyPort.Text = value.ToString();
            }
            get
            {
                int port = 0;
                int.TryParse(textProxyPort.Text, out port);
                return (port);
            }
        }

        /// <summary>
        /// Set the thumbnail space used
        /// </summary>
        public long ThumbnailCacheSpaceUsed
        {
            set
            {
                textThumbnailCacheSpaceUsed.Text = CalculateSpaceUsedText(value);
            }
        }

#if SILVERLIGHT
        /// <summary>
        /// Set the auth token
        /// </summary>
        public string AuthToken
        {
            set
            {
                _authToken = value;
            }
        }

        /// <summary>
        /// Set the application uri
        /// </summary>
        public string ApplicationUri
        {
            set
            {
                _applicationUri = value;
            }
        }
#endif
        #endregion

        #region Methods
#if SILVERLIGHT
        /// <summary>
        /// Set the license information
        /// </summary>
        /// <param name="licenseType">License type</param>
        /// <param name="licenseExpiryDateUtc">License expiry date</param>
        /// <param name="licenseUri">License uri</param>
        public void SetLicenseInformation(string licenseType, DateTime licenseExpiryDateUtc, Uri licenseUri)
        {
            buttonLicenseInformation.Visibility = Visibility.Visible;

            labelLicenseTypeValue.Text = licenseType;
            labelLicenseExpiryValue.Text = licenseExpiryDateUtc.ToLongDateString();

            TimeSpan ts = licenseExpiryDateUtc - DateTime.Now.ToUniversalTime();
            labelLicenseDaysRemainingValue.Text = ((int)Math.Ceiling(ts.TotalDays)).ToString();

            buttonPurchaseLicense.Visibility = ts.TotalDays < 30 ? Visibility.Visible : Visibility.Collapsed;

            _licenseUri = licenseUri;
        }
#endif

        /// <summary>
        /// Add configuration options for provider
        /// </summary>
        /// <param name="providerName">The provider</param>
        /// <param name="icon">Icon to display for provider</param>
        /// <param name="isAvailable">Is the configuration available</param>
        public void AddProviderConfiguration(string providerName, Uri icon, bool isAvailable)
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            if (icon != null)
            {
                Image image = new Image();
                image.Source = new System.Windows.Media.Imaging.BitmapImage(icon);
                image.Width = 32;
                image.Height = 32;
                sp.Children.Add(image);
            }
            TextBlock tb = new TextBlock();
            tb.Text = providerName;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Margin = new Thickness(5, 0, 0, 0);
            sp.Children.Add(tb);

            panelProviderConfigurationIcons.Children.Add(sp);

            StackPanel spButtons = new StackPanel();
            spButtons.Orientation = Orientation.Vertical;

            Button configureButton = new Button();
            configureButton.Height = 32;
            configureButton.Tag = providerName;
            configureButton.MaxWidth = 250;
            configureButton.Click += new RoutedEventHandler(configureButton_Click);

            TextBlock tb2 = new TextBlock();
            tb2.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CONFIGURE");
            configureButton.Content = tb2;

            Button configureDeleteButton = new Button();
            configureDeleteButton.Height = 32;
            configureDeleteButton.Tag = providerName;
            configureDeleteButton.MaxWidth = 250;
            configureDeleteButton.Click += new RoutedEventHandler(configureDeleteButton_Click);
            configureDeleteButton.IsEnabled = isAvailable;

            TextBlock tb3 = new TextBlock();
            tb3.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CONFIGURATIONDELETE");
            configureDeleteButton.Content = tb3;

            spButtons.Children.Add(configureButton);
            spButtons.Children.Add(configureDeleteButton);

            panelProviderConfigurationButtons.Children.Add(spButtons);
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelSettings.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SETTINGS");
            labelAccount.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "ACCOUNT");
            labelProviders.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "ACCOUNTS");
            labelLicenseInformation.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSEINFORMATION");

            labelLanguage.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LANGUAGE");

            labelHistory.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORY");
            labelHistoryToKeep.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYTOKEEP");
            labelHistorySpaceUsed.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYSPACEUSED");

            labelThumbnails.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILS");
            labelThumbnailsCacheSize.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILSIZE");
            labelThumbnailsCacheSpaceUsed.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILSPACEUSED");
            labelThumbnailCacheLocation.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILLOCATION");
            labelButtonThumbnailCacheEmpty.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILEMPTY");
            labelButtonThumbnailCacheOpen.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILLOCATIONOPEN");

            labelChangePassword.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORDCHANGE");
            labelCurrentPassword.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORDCURRENT");
            labelNewPassword.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORDNEW");
            labelButtonChangePassword.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORDCHANGE");

            foreach (StackPanel sp in panelProviderConfigurationButtons.Children)
            {
                ((sp.Children[0] as Button).Content as TextBlock).Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CONFIGURE");
                ((sp.Children[1] as Button).Content as TextBlock).Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CONFIGURATIONDELETE");
            }

            labelLicenseType.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSETYPE");
            labelLicenseExpiry.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSEEXPIRYDATE");
            labelLicenseDaysRemaining.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSEDAYSREMAINING");
            labelPurchaseLicenseButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSEPURCHASE");

            labelProxy.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXY");
            labelNoProxy.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYNONE");
            labelSystemProxy.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYSYSTEM");
            labelSystemProxyDefault.Text = "(" + CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYSYSTEMDEFAULT") + ")";
            labelSystemProxyInteretExplorer.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYSYSTEMINTERNETEXPLORER");
            labelWebProxy.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYMANUAL");
            labelProxyAddress.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYADDRESS");
            labelProxyPort.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROXYPORT");
        }
        #endregion

        #region Control Event Handlers

        private void listLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialising)
            {
                CultureHelper.CurrentCulture = Culture;
            }
        }

        void CultureHelper_CultureChanged(string culture)
        {
            Localise();
        }

        private void buttonChangePassword_Click(object sender, RoutedEventArgs e)
        {
#if SILVERLIGHT
            Obany.Cloud.Services.Proxy.AuthenticatedClient authenticatedClient = Obany.Cloud.Client.Model.ClientFactory.CreateService<Obany.Cloud.Services.Proxy.AuthenticatedClient>("Authenticated");
            if (authenticatedClient != null)
            {
                authenticatedClient.ChangePasswordCompleted += new EventHandler<Obany.Cloud.Services.Proxy.ChangePasswordCompletedEventArgs>(authenticatedClient_ChangePasswordCompleted);
                authenticatedClient.ChangePasswordAsync(_authToken, textCurrentPassword.Password, textNewPassword.Password, CultureHelper.CurrentCulture, _applicationUri + "ValidateAccount.aspx");
            }
#endif
        }

#if SILVERLIGHT
        void authenticatedClient_ChangePasswordCompleted(object sender, Obany.Cloud.Services.Proxy.ChangePasswordCompletedEventArgs e)
        {
            bool success = false;

            try
            {
                if (e.Result.Status == Obany.Core.OperationStatus.Success)
                {
                    success = true;
                }
            }
            catch(Exception exc)
            {
                Obany.Core.Logging.LogException("Configuration::authenticatedClient_ChangePasswordCompleted", exc, null, null, true);
            }

            if (success)
            {
                labelResult.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
                labelResult.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORDCHANGESUCCESS");
            }
            else
            {
                labelResult.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                labelResult.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORDCHANGEFAIL");
            }
        }
#endif

        void configureButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            if (b != null)
            {
                if (ConfigureProvider != null)
                {
                    ConfigureProvider((string)b.Tag);
                }
            }
        }

        void configureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            if (b != null)
            {
                if (ConfigureDeleteProvider != null)
                {
                    ConfigureDeleteProvider((string)b.Tag);
                }
            }
        }

        void _apiCredentials_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (StackPanel sp in panelProviderConfigurationButtons.Children)
            {
                Button deleteButton = sp.Children[1] as Button;
                string provider = (string)deleteButton.Tag;
                bool found = false;
                for (int i = 0; i < _apiCredentials.Count && !found; i++)
                {
                    if (_apiCredentials[i].Provider == provider)
                    {
                        found = true;
                    }
                }
                deleteButton.IsEnabled = found;
            }
        }

        private void SetMode(string mode)
        {
            contentControlSettings.Visibility = mode == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            contentControlAccount.Visibility = mode == "Account" ? Visibility.Visible : Visibility.Collapsed;
            contentControlProviders.Visibility = mode == "Providers" ? Visibility.Visible : Visibility.Collapsed;
            contentControlLicenseInformation.Visibility = mode == "License" ? Visibility.Visible : Visibility.Collapsed;
            contentControlProxy.Visibility = mode == "Proxy" ? Visibility.Visible : Visibility.Collapsed;

            buttonSettings.IsChecked = mode == "Settings";
            buttonAccount.IsChecked = mode == "Account";
            buttonProviders.IsChecked = mode == "Providers";
            buttonLicenseInformation.IsChecked = mode == "License";
            buttonProxy.IsChecked = mode == "Proxy";

            contentControlSettings.IsEnabled = contentControlSettings.Visibility == Visibility.Visible;
            contentControlAccount.IsEnabled = contentControlAccount.Visibility == Visibility.Visible;
            contentControlProviders.IsEnabled = contentControlProviders.Visibility == Visibility.Visible;
            contentControlLicenseInformation.IsEnabled = contentControlLicenseInformation.Visibility == Visibility.Visible;
            contentControlProxy.IsEnabled = contentControlProxy.Visibility == Visibility.Visible;
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            SetMode("Settings");
        }

        private void buttonAccount_Click(object sender, RoutedEventArgs e)
        {
            SetMode("Account");
        }

        private void buttonProviders_Click(object sender, RoutedEventArgs e)
        {
            SetMode("Providers");
        }

        private void buttonLicenseInformation_Click(object sender, RoutedEventArgs e)
        {
            SetMode("License");
        }

        private void buttonProxy_Click(object sender, RoutedEventArgs e)
        {
            SetMode("Proxy");
        }

        private void buttonPurchaseLicense_Click(object sender, RoutedEventArgs e)
        {
#if SILVERLIGHT
            if (OpenUrlRequested != null)
            {
                OpenUrlRequested(_licenseUri);
            }
#endif
        }

        private void buttonThumbnailCacheOpen_Click(object sender, RoutedEventArgs e)
        {
#if !SILVERLIGHT
            try
            {
                Process.Start(_thumbnailCacheLocation);
            }
            catch
            {
            }
#endif
        }

        private void buttonThumbnailCacheEmpty_Click(object sender, RoutedEventArgs e)
        {
            DialogPanel.ShowQuestionBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "THUMBNAILEMPTYQUESTION"), DialogButtons.Yes | DialogButtons.No,
            delegate(DialogResult dialogResult)
            {
                if (dialogResult == DialogResult.Yes)
                {
                    if (ThumbnailCacheClear != null)
                    {
                        ThumbnailCacheClear();
                    }
                }
            });
        }

        private void groupProxy_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            int pmode = GetProxyMode();

            textProxyAddress.IsEnabled = pmode == 2;
            textProxyPort.IsEnabled = pmode == 2;
        }

        private int GetProxyMode()
        {
            int pmode = 0;

            if (checkNoProxy.IsChecked.Value)
            {
                pmode = 1;
            }
            else if (checkSystemProxy.IsChecked.Value)
            {
                pmode = 0;
            }
            else if (checkWebProxy.IsChecked.Value)
            {
                pmode = 2;
            }

            return (pmode);
        }

        private void SetProxyMode(int value)
        {
            if (value == 1)
            {
                checkNoProxy.IsChecked = true;
            }
            else if (value == 0)
            {
                checkSystemProxy.IsChecked = true;
            }
            else if (value == 2)
            {
                checkWebProxy.IsChecked = true;
            }
        }
        #endregion

        #region Utility
        private string CalculateSpaceUsedText(long size)
        {
            string su = "";

            if (size < 1024)
            {
                su = size + " B";
            }
            else if (size < 1024 * 1024)
            {
                su = (size / 1024) + " KB";
            }
            else if (size < 1024 * 1024 * 1024)
            {
                su = Math.Round(((double)size / 1024.0 / 1024.0), 2) + " MB";
            }

            return(su);
        }
        #endregion
    }
}
