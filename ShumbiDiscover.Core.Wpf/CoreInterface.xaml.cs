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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Obany.Core;
using Obany.UI.Dialogs;
using System.Windows.Media.Imaging;
using ShumbiDiscover.Data;
using ShumbiDiscover.Controls;
using System.Windows.Controls.Primitives;
using ShumbiDiscover.Model;
using Obany.UI.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using Obany.Preferences.Model;

namespace ShumbiDiscover.Core
{
    /// <summary>
    /// Main page for application
    /// </summary>
    public partial class CoreInterface : UserControl
    {
        #region Fields
        private AboutData _aboutData;
        private CoreLogic _coreLogic;
        private string _waterMark;

        private bool _intialisingFilters;
        private List<string> _kindFilters;
        private List<string> _providerFilters;

        private SearchVisualisation _currentSearchVisualisation;

        private ProgressControl _progressControl;

        private DispatcherTimer _progressTimer;
        private bool _gettingProgress;

        private Guid _saveTaskId;
        private Guid _renderTaskId;
        private Guid _shareTaskId;

        private Queue<Control> _transitionQueueIn;
        private Queue<Control> _transitionQueueOut;
        private bool _transitionInProgress;

#if !SILVERLIGHT
        private int _proxyMode;
        private string _proxyAddress;
        private int _proxyPort;

        private string _lastSavePath;
#endif
        #endregion

        #region Constructors
        /// <summary>
        /// Static Constructor
        /// </summary>
        static CoreInterface()
        {
#if SILVERLIGHT
            CultureHelper.RegisterLanguageAssembly("ShumbiDiscover.Controls.%LANGUAGE%.resources.dll");
            CultureHelper.RegisterLanguageAssembly("ShumbiDiscover.Core.%LANGUAGE%.resources.dll");
            CultureHelper.RegisterLanguageAssembly("ShumbiDiscover.Notes.%LANGUAGE%.resources.dll");
            CultureHelper.RegisterLanguageAssembly("ShumbiDiscover.Visualisations.%LANGUAGE%.resources.dll");
            CultureHelper.RegisterLanguageAssembly("Obany.UI.%LANGUAGE%.resources.dll");
#endif

            RegisterThumbnailGenerators();
            RegisterSearchProviders();
            RegisterVisualisations();
        }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public CoreInterface()
        {
            InitializeComponent();

            buttonHistory.IsEnabled = false;
            buttonFavourites.IsEnabled = false;

            documentViewer.Visibility = Visibility.Collapsed;
            searchSelector.Visibility = Visibility.Collapsed;
            visualisationSelector.Visibility = Visibility.Collapsed;
            annotationCanvas.Visibility = Visibility.Collapsed;

            _transitionQueueIn = new Queue<Control>();
            _transitionQueueOut = new Queue<Control>();
            _transitionInProgress = false;

            searchSelector.SelectionChanged += new SearchSelector.SelectionChangedEventHandler(searchSelector_SelectionChanged);
            visualisationSelector.SelectionChanged += new VisualisationSelector.SelectionChangedEventHandler(visualisationSelector_SelectionChanged);
            searchVisualisationTab.ItemSelected += new SearchVisualisationTab.ItemSelectedEventHandler(searchVisualisationTab_ItemSelected);
            searchVisualisationTab.AddToFavourites += new SearchVisualisationTab.AddToFavouritesEventHandler(searchVisualisationTab_AddToFavourites);

            documentViewer.SelectionChanged += new ShumbiDiscover.Controls.DocumentViewer.SelectionChangedEventHandler(documentViewer_SelectionChanged);
            documentViewer.OpenUrlRequested += new ShumbiDiscover.Controls.DocumentViewer.OpenUrlRequestedEventHandler(OpenUrl);
            documentViewer.DisplayModeChanged += new ShumbiDiscover.Controls.DocumentViewer.DisplayModeChangedEventHandler(documentViewer_DisplayModeChanged);

            annotationCanvas.ExitAnnotation += new ShumbiDiscover.Notes.AnnotationCanvas.ExitAnnotationEventHandler(annotationCanvas_ExitAnnotation);
            annotationCanvas.SaveAnnotation += new ShumbiDiscover.Notes.AnnotationCanvas.SaveAnnotationEventHandler(annotationCanvas_SaveAnnotation);
            annotationCanvas.ShareAnnotation += new ShumbiDiscover.Notes.AnnotationCanvas.ShareAnnotationEventHandler(annotationCanvas_ShareAnnotation);

            _coreLogic = new CoreLogic();
            _coreLogic.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(CoreLogic_PropertyChanged);
            _coreLogic.CheckForDiskSpace += new AbstractCoreLogic.CheckForDiskSpaceComplete(CoreLogic_CheckForDiskSpace);

            _shareTaskId = Guid.Empty;
            _saveTaskId = Guid.Empty;
            _renderTaskId = Guid.Empty;

#if !SILVERLIGHT
            _proxyMode = 0;
            _proxyAddress = "";
            _proxyPort = 0;

            _lastSavePath = "";
#endif
        }
        #endregion

        #region Public Properties
#if SILVERLIGHT
        /// <summary>
        /// Set the auth token
        /// </summary>
        public string AuthToken
        {
            set
            {
                _coreLogic.AuthToken = value;
            }
        }

        /// <summary>
        /// Get the email address
        /// </summary>
        public string EmailAddress
        {
            get
            {
                return (_coreLogic.EmailAddress);
            }
        }

        /// <summary>
        /// Get the license type
        /// </summary>
        public int LicenseType
        {
            get
            {
                return (_coreLogic.LicenseType);
            }
        }

        /// <summary>
        /// Get the license expiry
        /// </summary>
        public DateTime LicenseExpiryUtc
        {
            get
            {
                return (_coreLogic.LicenseExpiryUtc);
            }
        }
#endif
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the application branding
        /// </summary>
        /// <param name="aboutData">The about information</param>
        /// <param name="aboutButtonResource">The resource to use for the about button</param>
        /// <param name="waterMark">Watermark for annotation</param>
        public void SetBranding(AboutData aboutData, Uri aboutButtonResource, string waterMark)
        {
            _aboutData = aboutData;
            if (aboutButtonResource != null)
            {
                Image image = new Image();
                image.Height = 48;
                image.Source = new BitmapImage(aboutButtonResource);
                buttonAboutImage.Content = image;
            }
            _waterMark = waterMark;
        }

        /// <summary>
        /// Set the flicks allowed flag
        /// </summary>
        /// <param name="allowed"></param>
        public void SetFlicksAllowed(bool allowed)
        {
            documentViewer.SetFlicksAllowed(allowed);
        }
        #endregion

        #region Initialisation and Closedown
        /// <summary>
        /// Initialise the control
        /// </summary>
        public void Initialise()
        {
#if !SILVERLIGHT
            // Hide the logout button if not silverlight
            buttonLogout.Visibility = Visibility.Collapsed;
            buttonLogout.IsEnabled = false;
            columnDefinitionLogout1.Width = new GridLength(0);
            columnDefinitionLogout2.Width = new GridLength(0);
            columnDefinitionLogout3.Width = new GridLength(0);
            columnDefinitionLogout4.Width = new GridLength(0);
#endif

            CultureHelper.CultureChanged += new CultureHelper.CultureChangedEventHandler(CultureHelper_CultureChanged);

            UpdateGoState();
            InformationPanelRefresh();
            ActionButtonsRefresh();

            _coreLogic.AccountStartSession(_aboutData.ProductName,
                delegate(bool success)
                {
#if !SILVERLIGHT
                    WebThumbnailGenerator.ThumbnailCacheFolder = _coreLogic.ThumbnailCacheLocation;
                    WebThumbnailGenerator.ThumbnailAdded += new WebThumbnailGenerator.ThumbnailAddedDelegate(WebThumbnailGenerator_ThumbnailAdded);
#endif

                    if (success)
                    {
                        gridInitialising.Visibility = Visibility.Collapsed;
#if SILVERLIGHT
                        labelLogoutButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LOGOUT");
#endif
                    }
                    else
                    {
                        _coreLogic.AccountEndSession();
                    }
                }
            );
        }

        void WebThumbnailGenerator_ThumbnailAdded(long size)
        {
            _coreLogic.ThumbnailCacheCleanup(size);
        }

        /// <summary>
        /// Closedown the interface
        /// </summary>
        public void Closedown()
        {
            _coreLogic.SelectedVisualisation = visualisationSelector.Visualisation;
            _coreLogic.SelectedSearchProviders = searchSelector.SearchProviders;
            _coreLogic.ConfigurationSave();

            ThumbnailManager.Instance.Cleanup();
        }

        /// <summary>
        /// Load settings
        /// </summary>
        /// <param name="preferencesProvider">The preferences provider</param>
        public void LoadSettings(IPreferencesProvider preferencesProvider)
        {
            if (preferencesProvider != null)
            {
#if !SILVERLIGHT
                _lastSavePath = preferencesProvider.GetString("LastSavePath", "");

                _proxyMode = preferencesProvider.GetInt32("ProxyMode", _proxyMode);
                _proxyAddress = preferencesProvider.GetString("ProxyAddress", _proxyAddress);
                _proxyPort = preferencesProvider.GetInt32("ProxyPort", _proxyPort);

                Obany.Communications.HttpChannel.SetProxy(_proxyMode, _proxyAddress, _proxyPort);
#endif
            }
        }

        /// <summary>
        /// Save settings
        /// </summary>
        /// <param name="preferencesProvider">The preferences provider</param>
        public void SaveSettings(IPreferencesProvider preferencesProvider)
        {
            if (preferencesProvider != null)
            {
#if !SILVERLIGHT
                preferencesProvider.SetString("LastSavePath", _lastSavePath);

                preferencesProvider.SetInt32("ProxyMode", _proxyMode);
                preferencesProvider.SetString("ProxyAddress", _proxyAddress);
                preferencesProvider.SetInt32("ProxyPort", _proxyPort);
#endif
            }
        }
        #endregion

        #region Application Control Panel
        private void UpdateGoState()
        {
            buttonSearch.IsEnabled = searchSelector.SearchProviders.Count > 0 && textQuery.Text.Trim().Length > 0;
        }
        #endregion

        #region Localisation Methods
        void CultureHelper_CultureChanged(string culture)
        {
            labelInitialising.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "INITIALISING") + "...";

            labelHistoryButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORY");
            labelFavouritesButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITES");

            labelSearch.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCH");

            labelSearchProvidersButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHENGINES");
            labelVisualisationsButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "VISUALISATIONS");

#if SILVERLIGHT
            labelLogoutButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LOGOUT");
#endif
            labelAboutButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "ABOUT");
            labelConfigurationButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CONFIGURATION");

            labelClusterInformation.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CLUSTERINFORMATION");
            labelActionInformation.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "ACTIONINFORMATION");

            labelExploreClusterButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CLUSTEREXPLORE");
            labelCloseClusterButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CLUSTERCLOSE");
            labelOpenSearchResultButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHRESULTOPEN");
            labelAnnotateSearchResultButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHRESULTANNOTATE");

            InformationPanelRefresh();
            ActionButtonsRefresh();

            documentViewer.Localise();
            annotationCanvas.Localise();
            searchVisualisationTab.Localise();
            searchSelector.Localise();
        }
        #endregion

        #region Account
        void Logout()
        {
            labelInitialising.Text = "";
            gridInitialising.Visibility = Visibility.Visible;

            _coreLogic.AccountEndSession();
        }
        #endregion

        #region Action Button Event Handlers
        private void buttonExploreCluster_Click(object sender, RoutedEventArgs e)
        {
            OpenDocumentViewer();
        }

        private void buttonCloseCluster_Click(object sender, RoutedEventArgs e)
        {
            CloseDocumentViewer();
        }

        private void buttonAnnotateSearchResult_Click(object sender, RoutedEventArgs e)
        {
            OpenAnnotationViewer();
        }

        void buttonOpenSearchResult_Click(object sender, RoutedEventArgs e)
        {
            SearchAggregateItem searchResultItem = documentViewer.SelectedItem;

            if (searchResultItem != null)
            {
                try
                {
                    Uri uri = new Uri(searchResultItem.OpenUrl);
                    OpenUrl(uri);
                }
                catch { }
            }
        }
        #endregion

        #region Application Toolbar Event Handlers
        private void buttonSearchProviders_Click(object sender, RoutedEventArgs e)
        {
            searchSelector.Visibility = Visibility.Visible;
            visualisationSelector.Visibility = Visibility.Collapsed;
        }

        private void buttonVisualisations_Click(object sender, RoutedEventArgs e)
        {
            visualisationSelector.Visibility = Visibility.Visible;
            searchSelector.Visibility = Visibility.Collapsed;
        }

        private void buttonSearchProviders_MouseMove(object sender, MouseEventArgs e)
        {
            searchSelector.ShowHide(true);
            visualisationSelector.ShowHide(false);
        }

        private void buttonVisualisations_MouseMove(object sender, MouseEventArgs e)
        {
            visualisationSelector.ShowHide(true);
            searchSelector.ShowHide(false);
        }

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            About dialog = new About();

            dialog.AboutData = _aboutData;

            dialog.SetCreditsInformation(CultureHelper.GetString(Properties.CreditsResource.ResourceManager, "CREDITS"));

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "ABOUT", dialog, "buttonOK", DialogButtons.Close, null);
        }

        private void buttonConfiguration_Click(object sender, RoutedEventArgs e)
        {
            string provider = "Scribd";
            Uri providerIconUrl = new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Scribd48.png", UriKind.Relative);

            ShumbiDiscover.Controls.Dialogs.Configuration dialog = new ShumbiDiscover.Controls.Dialogs.Configuration();

            dialog.Languages = CultureHelper.Languages;
            dialog.Culture = CultureHelper.CurrentCulture;
#if SILVERLIGHT
            dialog.AuthToken = _coreLogic.AuthToken;
            dialog.ApplicationUri = _coreLogic.ApplicationUri;
#endif

            dialog.ApiCredentials = _coreLogic.ApiCredentials;
            dialog.AddProviderConfiguration(provider, providerIconUrl, _coreLogic.ApiCredentialsExist(provider));

            dialog.HistoryItemCount = _coreLogic.HistoryItemsToKeep;
            dialog.HistorySpaceUsed = _coreLogic.HistorySpaceUsed();

            dialog.ThumbnailCacheSize = _coreLogic.ThumbnailCacheSize;
            dialog.ThumbnailCacheLocation = _coreLogic.ThumbnailCacheLocation;
            dialog.ThumbnailCacheSpaceUsed = _coreLogic.ThumbnailCacheSpaceUsed();

#if !SILVERLIGHT
            dialog.ProxyMode = _proxyMode;
            dialog.ProxyAddress = _proxyAddress;
            dialog.ProxyPort = _proxyPort;
#endif

            dialog.ConfigureProvider += delegate(string provider2)
            {
                ConfigureProvider(provider2, null);
            };

            dialog.ConfigureDeleteProvider += delegate(string provider2)
            {
                _coreLogic.ApiCredentialsDelete(provider2,
                    delegate(bool success)
                    {
                        DialogPanel.ShowInformationBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "CONFIGURATIONDELETED"), DialogButtons.Ok, null);
                    }
                );
            };

            dialog.ThumbnailCacheClear += delegate()
            {
                _coreLogic.ThumbnailCacheClear();
                dialog.ThumbnailCacheSpaceUsed = _coreLogic.ThumbnailCacheSpaceUsed();
            };

#if SILVERLIGHT
            dialog.OpenUrlRequested += new ShumbiDiscover.Controls.Dialogs.Configuration.OpenUrlRequestedEventHandler(OpenUrl);

            string licenseType = "";

            if (_coreLogic.LicenseType == 1000)
            {
                licenseType = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSEEVALUATION");
            }
            else if (_coreLogic.LicenseType == 2000)
            {
                licenseType = CultureHelper.GetString(Properties.Resources.ResourceManager, "LICENSEANNUAL");
            }

            Uri licenseUri = new Uri(_coreLogic.ApplicationUri + "Licensing.aspx?emailaddress=" + _coreLogic.EmailAddress + "&culture=" + CultureHelper.CurrentCulture);

            dialog.SetLicenseInformation(licenseType, _coreLogic.LicenseExpiryUtc, licenseUri);
#endif

            string oldCulture = CultureHelper.CurrentCulture;

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "CONFIGURATION", dialog, "buttonOK", DialogButtons.Ok | DialogButtons.Cancel,
                delegate(DialogResult dialogResult)
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        // If the culture has been changed, then save preferences
                        if (oldCulture != CultureHelper.CurrentCulture)
                        {
                            _coreLogic.AccountSetCulture(CultureHelper.CurrentCulture);
                        }

                        _coreLogic.ThumbnailCacheSize = dialog.ThumbnailCacheSize;
                        _coreLogic.ThumbnailCacheCleanup(0);

                        _coreLogic.HistoryItemsToKeep = dialog.HistoryItemCount;
                        _coreLogic.HistoryCleanup();

#if !SILVERLIGHT
                        _proxyAddress = dialog.ProxyAddress;
                        _proxyPort = dialog.ProxyPort;
                        _proxyMode = dialog.ProxyMode;

                        Obany.Communications.HttpChannel.SetProxy(_proxyMode, _proxyAddress, _proxyPort);
#endif

                        _coreLogic.ConfigurationSave();
                    }
                    else
                    {
                        // The culture was changed but then dialog was cancelled
                        // so reset to the old culture
                        if (CultureHelper.CurrentCulture != oldCulture)
                        {
                            CultureHelper.CurrentCulture = oldCulture;
                        }
                    }
                }
            );
        }

        private void buttonHistory_Click(object sender, RoutedEventArgs e)
        {
            ShumbiDiscover.Controls.Dialogs.History dialog = new ShumbiDiscover.Controls.Dialogs.History();

            dialog.SearchHistory = _coreLogic.History;

            dialog.HistoryOpenItem += delegate(SearchDescription searchDescription)
            {
                DialogPanel.Close(dialog, DialogResult.Close);

                if (!searchVisualisationTab.SelectSearchVisualisation(searchDescription.Id))
                {
                    SearchVisualisation searchVisualisation = new SearchVisualisation();
                    searchVisualisation.Visualisation = visualisationSelector.Visualisation;
                    searchVisualisation.Localise();
                    searchVisualisation.ItemActivated += new SearchVisualisation.ItemActivatedEventHandler(SearchVisualisation_ItemActivated);
                    searchVisualisation.ItemSelected += new SearchVisualisation.ItemSelectedEventHandler(SearchVisualisation_ItemSelected);

                    searchVisualisation.SearchResultSet = new SearchResultSet();
                    searchVisualisation.SearchResultSet.SearchDescription = searchDescription;

                    textQuery.Text = searchDescription.Query;
                    DisplayNewVisualisation(searchVisualisation);
                    UpdateGoState();

                    _currentSearchVisualisation = searchVisualisation;

                    searchVisualisationTab.AddSearchVisualisation(_currentSearchVisualisation);

                    ShowSearchResultProgressDialog(CultureHelper.GetString(Properties.Resources.ResourceManager, "LOADING") + "...");

                    _coreLogic.SearchResultSetLoad(searchDescription,
                        delegate(SearchResultSet searchResultSet)
                        {
                            Action a = delegate()
                            {
                                if (searchResultSet != null && _currentSearchVisualisation != null)
                                {
                                    _currentSearchVisualisation.SearchResultSet = searchResultSet;
                                    _currentSearchVisualisation.PopulateVisualisation();
                                    DialogPanel.Close(_progressControl, DialogResult.Ok);
                                }
                                else
                                {
                                    DialogPanel.Close(_progressControl, DialogResult.Cancel);
                                }
                            };
#if SILVERLIGHT
                            Dispatcher.BeginInvoke(a);
#else
                            Dispatcher.Invoke(a);
#endif
                        }
                     );

                }
            };

            dialog.HistoryDeleteItem += delegate(SearchDescription searchDescription)
            {
                _coreLogic.HistoryDelete(searchDescription);
            };

            dialog.HistoryClearItems += delegate()
            {
                _coreLogic.HistoryClear();
            };

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "HISTORY", dialog, "buttonClose", DialogButtons.Close, null);
        }

        private void DisplayNewVisualisation(SearchVisualisation searchVisualisation)
        {
            if (_currentSearchVisualisation != null)
            {
                _currentSearchVisualisation.VisualisationStateChanged(false);
            }
            if (searchVisualisation != null)
            {
                contentPresenterVisualisation.Content = searchVisualisation.VisualisationControl;
                searchVisualisation.VisualisationStateChanged(true);
            }
            else
            {
                contentPresenterVisualisation.Content = null;
            }
        }

        private void buttonFavourites_Click(object sender, RoutedEventArgs e)
        {
            ShumbiDiscover.Controls.Dialogs.Favourites dialog = new ShumbiDiscover.Controls.Dialogs.Favourites();

            dialog.RootFavourites = _coreLogic.Favourites;

            dialog.OpenFavourite += delegate(Favourite favourite)
            {
                DialogPanel.Close(dialog, DialogResult.Close);

                searchSelector.SearchProviders = favourite.SearchProviders;
                visualisationSelector.Visualisation = favourite.Visualisation;
                textQuery.Text = favourite.Query;
                UpdateGoState();

                SearchVisualise(textQuery.Text);
            };

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "FAVOURITES", dialog, "buttonClose", DialogButtons.Close,
                delegate(DialogResult dialogControlResult)
                {
                    _coreLogic.FavouritesSave();
                }
            );
        }

        private void textQuery_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateGoState();

            if (e.Key == Key.Enter)
            {
                if (buttonSearch.IsEnabled)
                {
                    SearchVisualise(textQuery.Text.Trim());
                }
            }
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchVisualise(textQuery.Text.Trim());
        }

        private void buttonLogout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }
        #endregion

        #region Information Panel
        void InformationPanelRefresh()
        {
            bool populated = false;

            _intialisingFilters = true;
            _kindFilters = new List<string>();
            _providerFilters = new List<string>();
            panelInformation.Children.Clear();

            if (_currentSearchVisualisation != null)
            {
                if (_currentSearchVisualisation.CurrentCluster != null)
                {
                    bool useButtons = _currentSearchVisualisation.IsClusterOpen;

                    populated = true;

                    Dictionary<string, int> searchProviders = new Dictionary<string, int>();
                    Dictionary<string, int> searchResultKinds = new Dictionary<string, int>();
                    Collection<Guid> alreadyAdded = new Collection<Guid>();
                    GetClusterInformation(_currentSearchVisualisation.CurrentCluster, searchProviders, searchResultKinds, alreadyAdded);

                    TextBlock tbKinds = new TextBlock();
                    tbKinds.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHRESULTTYPES");
                    tbKinds.FontSize = 14;
                    tbKinds.Margin = new Thickness(0, 0, 0, 5);

                    panelInformation.Children.Add(tbKinds);

                    foreach (KeyValuePair<string, int> kvp in searchResultKinds)
                    {
                        StackPanel sp = new StackPanel();
                        sp.Orientation = Orientation.Horizontal;
                        sp.Margin = new Thickness(10, 0, 0, 2);

                        Image icon = new Image();
                        icon.Width = 32;
                        icon.Height = 32;
                        icon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/Kinds/" + kvp.Key + "32.png", UriKind.Relative));

                        TextBlock tbKind = new TextBlock();
                        tbKind.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHRESULTTYPE" + kvp.Key.ToUpper()) + " [" + kvp.Value + "]";
                        tbKind.Margin = new Thickness(10, 0, 0, 0);
                        tbKind.VerticalAlignment = VerticalAlignment.Center;

                        sp.Children.Add(icon);
                        sp.Children.Add(tbKind);

                        if (useButtons)
                        {
                            ToggleButton button = new ToggleButton();
                            button.Height = 40;
                            button.Width = 200;
                            button.Checked += new RoutedEventHandler(kindButton_Checked);
                            button.Unchecked += new RoutedEventHandler(kindButton_Unchecked);
                            button.Content = sp;
                            button.IsChecked = true;
                            button.Tag = kvp.Key;
                            panelInformation.Children.Add(button);

                            _kindFilters.Add(kvp.Key);
                        }
                        else
                        {
                            panelInformation.Children.Add(sp);
                        }
                    }

                    TextBlock tbSearchProviders = new TextBlock();
                    tbSearchProviders.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHENGINES");
                    tbSearchProviders.FontSize = 14;
                    tbSearchProviders.Margin = new Thickness(0, 10, 0, 5);

                    panelInformation.Children.Add(tbSearchProviders);

                    foreach (KeyValuePair<string, int> kvp in searchProviders)
                    {
                        StackPanel sp = new StackPanel();
                        sp.Orientation = Orientation.Horizontal;
                        sp.Margin = new Thickness(10, 0, 0, 2);

                        Image icon = new Image();
                        icon.Width = 32;
                        icon.Height = 32;
                        icon.Source = new System.Windows.Media.Imaging.BitmapImage(SearchProviderFactory.GetLargeImage(kvp.Key));

                        TextBlock tbSearchProvider = new TextBlock();
                        tbSearchProvider.Text = kvp.Key + " [" + kvp.Value + "]";
                        tbSearchProvider.Margin = new Thickness(10, 0, 0, 0);
                        tbSearchProvider.VerticalAlignment = VerticalAlignment.Center;

                        sp.Children.Add(icon);
                        sp.Children.Add(tbSearchProvider);

                        if (useButtons)
                        {
                            ToggleButton button = new ToggleButton();
                            button.Height = 40;
                            button.Width = 200;
                            button.Checked += new RoutedEventHandler(providerButton_Checked);
                            button.Unchecked += new RoutedEventHandler(providerButton_Unchecked);
                            button.Content = sp;
                            button.IsChecked = true;
                            button.Tag = kvp.Key;
                            panelInformation.Children.Add(button);

                            _providerFilters.Add(kvp.Key);
                        }
                        else
                        {
                            panelInformation.Children.Add(sp);
                        }
                    }
                }
            }

            if (!populated)
            {
                TextBlock tbNone = new TextBlock();
                tbNone.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "CLUSTERSNONESELECTED");

                panelInformation.Children.Add(tbNone);
            }

            _intialisingFilters = false;
        }

        void kindButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_intialisingFilters)
            {
                string kind = (string)(sender as ToggleButton).Tag;

                if (_kindFilters.Contains(kind))
                {
                    _kindFilters.Remove(kind);
                    documentViewer.Populate(_currentSearchVisualisation.CurrentCluster, _currentSearchVisualisation.SearchResultsDictionary, _currentSearchVisualisation.DocumentViewerDisplayMode, _kindFilters, _providerFilters);
                }
            }
        }

        void kindButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!_intialisingFilters)
            {
                string kind = (string)(sender as ToggleButton).Tag;

                if (!_kindFilters.Contains(kind))
                {
                    _kindFilters.Add(kind);
                    documentViewer.Populate(_currentSearchVisualisation.CurrentCluster, _currentSearchVisualisation.SearchResultsDictionary, _currentSearchVisualisation.DocumentViewerDisplayMode, _kindFilters, _providerFilters);
                }
            }
        }

        void providerButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_intialisingFilters)
            {
                string provider = (string)(sender as ToggleButton).Tag;

                if (_providerFilters.Contains(provider))
                {
                    _providerFilters.Remove(provider);
                    documentViewer.Populate(_currentSearchVisualisation.CurrentCluster, _currentSearchVisualisation.SearchResultsDictionary, _currentSearchVisualisation.DocumentViewerDisplayMode, _kindFilters, _providerFilters);
                }
            }
        }

        void providerButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!_intialisingFilters)
            {
                string provider = (string)(sender as ToggleButton).Tag;

                if (!_providerFilters.Contains(provider))
                {
                    _providerFilters.Add(provider);
                    documentViewer.Populate(_currentSearchVisualisation.CurrentCluster, _currentSearchVisualisation.SearchResultsDictionary, _currentSearchVisualisation.DocumentViewerDisplayMode, _kindFilters, _providerFilters);
                }
            }
        }

        private void GetClusterInformation(SearchCluster cluster, Dictionary<string, int> searchProviders, Dictionary<string, int> searchResultKinds, Collection<Guid> alreadyAdded)
        {
            if (cluster.SearchResultIds != null)
            {
                foreach (Guid searchResultId in cluster.SearchResultIds)
                {
                    if (!alreadyAdded.Contains(searchResultId))
                    {
                        alreadyAdded.Add(searchResultId);

                        SearchAggregateItem searchAggregateItem = _currentSearchVisualisation.SearchResultsDictionary[searchResultId];

                        foreach (SearchProviderRank searchProviderRank in searchAggregateItem.ProviderRanks)
                        {
                            if (!searchProviders.ContainsKey(searchProviderRank.Provider))
                            {
                                searchProviders.Add(searchProviderRank.Provider, 1);
                            }
                            else
                            {
                                searchProviders[searchProviderRank.Provider]++;
                            }
                        }

                        if (!searchResultKinds.ContainsKey(searchAggregateItem.Kind))
                        {
                            searchResultKinds.Add(searchAggregateItem.Kind, 1);
                        }
                        else
                        {
                            searchResultKinds[searchAggregateItem.Kind]++;
                        }
                    }
                }
            }
            if (cluster.SearchClusters != null)
            {
                foreach (SearchCluster subCluster in cluster.SearchClusters)
                {
                    GetClusterInformation(subCluster, searchProviders, searchResultKinds, alreadyAdded);
                }
            }
        }
        #endregion

        #region Action Buttons
        void ActionButtonsRefresh()
        {
            bool isClusterOpen = false;
            bool hasCluster = false;
            bool hasItem = false;

            if (_currentSearchVisualisation != null)
            {
                isClusterOpen = _currentSearchVisualisation.IsClusterOpen;
                hasCluster = _currentSearchVisualisation.CurrentCluster != null;
                hasItem = _currentSearchVisualisation.CurrentSearchAggregateItem != null;
            }

            buttonExploreCluster.IsEnabled = hasCluster && !isClusterOpen;
            buttonCloseCluster.IsEnabled = hasCluster && isClusterOpen;
            buttonOpenSearchResult.IsEnabled = hasItem && isClusterOpen;
            buttonAnnotateSearchResult.IsEnabled = hasItem && isClusterOpen;

            panelVisualisationButtons.Visibility = isClusterOpen ? Visibility.Collapsed : Visibility.Visible;
            panelDocumentViewerButtons.Visibility = isClusterOpen ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion

        #region Search Selector Event Handlers
        void searchSelector_SelectionChanged(List<string> searchProviders)
        {
            _coreLogic.SelectedSearchProviders = searchProviders;
            UpdateGoState();
        }
        #endregion

        #region Visualisation Selector Event Handlers
        void visualisationSelector_SelectionChanged(string visualisation)
        {
            _coreLogic.SelectedVisualisation = visualisation;

            CloseDocumentViewer();

            visualisationSelector.Visibility = Visibility.Collapsed;
            if (_currentSearchVisualisation != null)
            {
                if (_currentSearchVisualisation.Visualisation != visualisation)
                {
                    _currentSearchVisualisation.CurrentCluster = null;
                    _currentSearchVisualisation.Visualisation = visualisation;
                    _currentSearchVisualisation.Localise();
                    searchVisualisationTab.RefreshSearchVisualisation(_currentSearchVisualisation);
                    DisplayNewVisualisation(_currentSearchVisualisation);
                }
            }

            InformationPanelRefresh();
            ActionButtonsRefresh();
        }
        #endregion

        #region Search Visualiation Tab Event Handlers
        void searchVisualisationTab_ItemSelected(SearchVisualisation searchVisualisation)
        {
            bool showDocumentViewer = false;

            DisplayNewVisualisation(searchVisualisation);

            _currentSearchVisualisation = searchVisualisation;

            if (_currentSearchVisualisation != null)
            {
                textQuery.Text = _currentSearchVisualisation.SearchResultSet.SearchDescription.Query;
                showDocumentViewer = _currentSearchVisualisation.IsClusterOpen;
            }

            if (showDocumentViewer)
            {
                OpenDocumentViewer();
            }
            else
            {
                CloseDocumentViewer();
            }

            InformationPanelRefresh();
            ActionButtonsRefresh();
            UpdateGoState();
        }

        void searchVisualisationTab_AddToFavourites(SearchVisualisation searchVisualisation)
        {
            AddToFavourites(searchVisualisation);
        }

        private void AddToFavourites(SearchVisualisation searchVisualisation)
        {
            if (searchVisualisation != null)
            {
                Favourite favourite = new Favourite();
                favourite.Name = searchVisualisation.SearchResultSet.SearchDescription.Query;
                favourite.Query = searchVisualisation.SearchResultSet.SearchDescription.Query;
                favourite.SearchProviders = searchVisualisation.SearchResultSet.SearchDescription.SearchProviders;
                favourite.Visualisation = searchVisualisation.Visualisation;

                _coreLogic.FavouritesAdd(favourite);
            }
        }
        #endregion

        #region Document Viewer
        void Transition(Control outItem, Control inItem)
        {
            if (_transitionInProgress)
            {
                _transitionQueueOut.Enqueue(outItem);
                _transitionQueueIn.Enqueue(inItem);
            }
            else
            {
                _transitionInProgress = true;

                inItem.Opacity = 0;
                inItem.Visibility = Visibility.Visible;
                outItem.IsEnabled = false;

                DoubleAnimation daFadeOut = new DoubleAnimation();
                daFadeOut.To = 0;
                daFadeOut.Duration = TimeSpan.FromMilliseconds(1000);
                daFadeOut.Completed += delegate(object sender, EventArgs e)
                {
                    inItem.IsEnabled = true;
                    outItem.Visibility = Visibility.Collapsed;
                    _transitionInProgress = false;

                    if (_transitionQueueIn.Count > 0)
                    {
                        Transition(_transitionQueueOut.Dequeue(), _transitionQueueIn.Dequeue());
                    }
                };

                DoubleAnimation daFadeIn = new DoubleAnimation();
                daFadeIn.To = 1;
                daFadeIn.Duration = TimeSpan.FromMilliseconds(1000);

                outItem.BeginAnimation(FrameworkElement.OpacityProperty, daFadeOut);
                inItem.BeginAnimation(FrameworkElement.OpacityProperty, daFadeIn);
            }
        }

        private void OpenDocumentViewer()
        {
            _currentSearchVisualisation.IsClusterOpen = true;
            _currentSearchVisualisation.CurrentSearchAggregateItem = null;
            _currentSearchVisualisation.VisualisationStateChanged(false);
            contentPresenterVisualisation.Visibility = Visibility.Collapsed;

            documentViewer.Cleanup();
            documentViewer.Visibility = Visibility.Visible;

            ThumbnailManager.Instance.Initialize();

            InformationPanelRefresh();
            ActionButtonsRefresh();

            documentViewer.Populate(_currentSearchVisualisation.CurrentCluster, _currentSearchVisualisation.SearchResultsDictionary, _currentSearchVisualisation.DocumentViewerDisplayMode, _kindFilters, _providerFilters);
        }

        private void CloseDocumentViewer()
        {
            ThumbnailManager.Instance.Cleanup();

            if (_currentSearchVisualisation != null)
            {
                _currentSearchVisualisation.IsClusterOpen = false;
                _currentSearchVisualisation.CurrentSearchAggregateItem = null;
                _currentSearchVisualisation.VisualisationStateChanged(true);
            }
            contentPresenterVisualisation.Visibility = Visibility.Visible;

            documentViewer.Visibility = Visibility.Collapsed;
            documentViewer.Cleanup();

            InformationPanelRefresh();
            ActionButtonsRefresh();
        }
        #endregion

        #region Document Viewer Event Handlers
        void documentViewer_SelectionChanged(object sender, ShumbiDiscover.Controls.EventArgs.SelectionChangedEventArgs e)
        {
            if (_currentSearchVisualisation != null)
            {
                _currentSearchVisualisation.CurrentSearchAggregateItem = e.NewValue;
            }
            ActionButtonsRefresh();
        }

        void documentViewer_DisplayModeChanged(DocumentViewerDisplayMode displayMode)
        {
            if (_currentSearchVisualisation != null)
            {
                _currentSearchVisualisation.DocumentViewerDisplayMode = displayMode;
            }
        }
        #endregion

        #region Annotation Viewer Event Handlers
        void annotationCanvas_ExitAnnotation()
        {
            CloseAnnotationViewer();
        }

        void annotationCanvas_SaveAnnotation(Obany.Render.Objects.Canvas canvas, string mimeType)
        {
            _progressControl = new ProgressControl();

            AddWatermark(canvas);

            string files = CultureHelper.GetString(Properties.Resources.ResourceManager, "FILES");
            string allFiles = CultureHelper.GetString(Properties.Resources.ResourceManager, "ALLFILES");

            string ext = "";
            string filter = "";
            if (mimeType == "image/jpeg")
            {
                ext += ".jpg";
                filter = "Jpg " + files + " (*.jpg)|*.jpg";
                _progressControl.Status = CultureHelper.GetString(Properties.Resources.ResourceManager, "CREATING") + " Jpg";
            }
            else if (mimeType == "application/vnd.ms-xpsdocument")
            {
                ext += ".xps";
                filter = "Xps " + files + " (*.xps)|*.xps";
                _progressControl.Status = CultureHelper.GetString(Properties.Resources.ResourceManager, "CREATING") + " Xps";
            }
            else if (mimeType == "application/pdf")
            {
                ext += ".pdf";
                filter = "Pdf " + files + " (*.pdf)|*.pdf";
                _progressControl.Status = CultureHelper.GetString(Properties.Resources.ResourceManager, "CREATING") + " Pdf";
            }

            filter += "|" + allFiles + " (*.*)|*.*";

#if SILVERLIGHT
            SaveFileDialog saveFileDialog = new SaveFileDialog();
#else
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            if (!string.IsNullOrEmpty(_lastSavePath))
            {
                saveFileDialog.InitialDirectory = _lastSavePath;
            }
#endif
            saveFileDialog.DefaultExt = ext;
            saveFileDialog.Filter = filter;

            if (saveFileDialog.ShowDialog().Value)
            {
#if !SILVERLIGHT
                _lastSavePath = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
#endif

                System.IO.Stream saveStream = saveFileDialog.OpenFile();

                DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "PROGRESS", _progressControl, "buttonCancel", DialogButtons.Cancel,
                    delegate(DialogResult dialogControlResult)
                    {
                        _progressControl = null;
                    }
                );

                _saveTaskId = Guid.NewGuid();

                _coreLogic.AnnotationSave(_saveTaskId, saveStream, canvas, mimeType,
                    delegate(bool success, Guid saveTaskIdComplete)
                {
                    Action a = delegate()
                    {
                        if (_saveTaskId == saveTaskIdComplete)
                        {
                            if (!success)
                            {
                                DialogPanel.ShowInformationBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "ANNOTATIONUNABLETOSAVE"),
                                                            DialogButtons.Ok, null);
                            }
                            else
                            {
                                if (annotationCanvas != null)
                                {
                                    annotationCanvas.ResetChanges();
                                }
                            }

                            if (_progressControl != null)
                            {
                                DialogPanel.Close(_progressControl, DialogResult.Close);
                            }
                            _saveTaskId = Guid.Empty;
                        }
                    };

#if SILVERLIGHT
                    Dispatcher.BeginInvoke(a);
#else
                    Dispatcher.Invoke(a);
#endif
                });
            }
        }

        private void AddWatermark(Obany.Render.Objects.Canvas canvas)
        {
            if (!string.IsNullOrEmpty(_waterMark))
            {
                string fullWaterMark = "";
                double fontSize = 24;
                for (int i = 0; i < canvas.Width / (_waterMark.Length * FontSize); i++)
                {
                    fullWaterMark += _waterMark + " - ";
                }
                for (int i = 0; i < canvas.Height / 140; i++)
                {
                    Obany.Render.Objects.TextBlock tbWatermark = new Obany.Render.Objects.TextBlock();
                    tbWatermark.Left = 0;
                    tbWatermark.Top = (i * 140) + 60;
                    tbWatermark.Text = fullWaterMark;
                    tbWatermark.FontSize = fontSize;
                    tbWatermark.FontName = "Arial";
                    tbWatermark.FontColour = "#22000000";

                    canvas.Children.Add(tbWatermark);
                }
            }
        }

        void annotationCanvas_ShareAnnotation(Obany.Render.Objects.Canvas canvas, string provider)
        {
            // Do we have any stored credentials for the provider
            if (!_coreLogic.ApiCredentialsExist(provider))
            {
                ConfigureProvider(provider, canvas);
            }
            else
            {
                // We have credentials so let the renderer do an upload
                AnnotationShare(provider, canvas);
            }
        }
        #endregion

        #region Annotation Viewer
        private void OpenAnnotationViewer()
        {
            annotationCanvas.ClearContent();

            Transition(contentPresenterSearchVisualise, annotationCanvas);

            annotationCanvas.Title = _currentSearchVisualisation.CurrentSearchAggregateItem.Title;

            _progressControl = new ProgressControl();
            _progressControl.Status = CultureHelper.GetString(Properties.Resources.ResourceManager, "RENDERING") + "...";

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "PROGRESS", _progressControl, "buttonCancel", DialogButtons.Cancel,
                delegate(DialogResult dialogControlResult)
                {
                    _progressControl = null;
                    if (dialogControlResult == DialogResult.Cancel)
                    {
                        CloseAnnotationViewer();
                    }
                }
            );

            string annotateUrl = _currentSearchVisualisation.CurrentSearchAggregateItem.ContentUrl;
            bool showHeader = false;

            if (string.IsNullOrEmpty(annotateUrl))
            {
                showHeader = true;
                annotateUrl = _currentSearchVisualisation.CurrentSearchAggregateItem.OpenUrl;
            }

            _renderTaskId = Guid.NewGuid();

            _coreLogic.AnnotationRenderWebPage(_renderTaskId, annotateUrl, 
                delegate(bool success, Guid renderTaskIdComplete, byte[] imageData, int imageWidth, int imageHeight, string imageMimeType, string renderDataId)
                {
                    Action a = delegate()
                    {
                        if (_renderTaskId == renderTaskIdComplete)
                        {
                            if (!success)
                            {
                                DialogPanel.ShowInformationBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "ANNOTATIONUNABLETORETRIEVESEARCHRESULT"), DialogButtons.Ok, null);
                            }
                            else
                            {
                                if (annotationCanvas != null)
                                {
                                    if (showHeader)
                                    {
                                        annotationCanvas.SetupCanvas(imageWidth + 40, imageHeight + 80);
                                        if (_currentSearchVisualisation != null)
                                        {
                                            annotationCanvas.AddText(_currentSearchVisualisation.CurrentSearchAggregateItem.Title + " [" + _currentSearchVisualisation.CurrentSearchAggregateItem.OpenUrl + "]", 14, "Arial", 20, 20);
                                        }
                                        annotationCanvas.AddRectangle(Color.FromArgb(0xFF, 0x99, 0x99, 0x99), imageWidth, 2, 20, 42);
                                        annotationCanvas.AddBitmapImage(imageData, imageWidth, imageHeight, imageMimeType, renderDataId, 20, 60);
                                    }
                                    else
                                    {
                                        annotationCanvas.SetupCanvas(imageWidth, imageHeight);
                                        annotationCanvas.AddBitmapImage(imageData, imageWidth, imageHeight, imageMimeType, renderDataId, 0, 0);
                                    }
                                }
                            }

                            if (_progressControl != null)
                            {
                                DialogPanel.Close(_progressControl, success ? DialogResult.Ok : DialogResult.Cancel);
                            }

                            _renderTaskId = Guid.Empty;
                        }
                    };
#if SILVERLIGHT
                    Dispatcher.BeginInvoke(a);
#else
                    Dispatcher.Invoke(a);
#endif
                }
             );
        }

        private void CloseAnnotationViewer()
        {
            annotationCanvas.ClearContent();

            _coreLogic.AnnotationCleanup();

            Transition(annotationCanvas, contentPresenterSearchVisualise);
        }

        private void ConfigureProvider(string provider, Obany.Render.Objects.Canvas canvas)
        {
            // Nope better prompt for some
            ShumbiDiscover.Controls.Dialogs.ApiLogin apiLoginDialog = new ShumbiDiscover.Controls.Dialogs.ApiLogin();

            apiLoginDialog.Provider = "Scribd";
            apiLoginDialog.ProviderIcon = new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Scribd48.png", UriKind.Relative);
            apiLoginDialog.ProviderLoginInformation = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROVIDERLOGIN").Replace("%1", apiLoginDialog.Provider);
            apiLoginDialog.ProviderSignupInformation = CultureHelper.GetString(Properties.Resources.ResourceManager, "PROVIDERSIGNUP").Replace("%1", apiLoginDialog.Provider);
            apiLoginDialog.ProviderSignupUrl = new Uri("http://www.scribd.com");

            apiLoginDialog.OpenUrlRequested += new ShumbiDiscover.Controls.Dialogs.ApiLogin.OpenUrlRequestedEventHandler(OpenUrl);

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "CONFIGURATION", apiLoginDialog, "buttonOK", DialogButtons.Ok | DialogButtons.Cancel,
                delegate(DialogResult dialogControlResult)
                {
                    if (dialogControlResult == DialogResult.Ok)
                    {
                        string apiCredentials1 = apiLoginDialog.ApiCredentials1;
                        string apiCredentials2 = apiLoginDialog.ApiCredentials2;
                        string apiCredentials3 = "";
                        string apiCredentials4 = "";

                        _coreLogic.ApiCredentialsSet(provider, apiCredentials1, apiCredentials2, apiCredentials3, apiCredentials4,
                            delegate(bool success)
                            {
                                if (success)
                                {
                                    if (canvas != null)
                                    {
                                        AnnotationShare(provider, canvas);
                                    }
                                }
                            }
                         );
                    }
                }
            );
        }

        internal void AnnotationShare(string provider, Obany.Render.Objects.Canvas canvas)
        {
            ShumbiDiscover.Controls.Dialogs.DocumentInformation documentInformationDialog = new ShumbiDiscover.Controls.Dialogs.DocumentInformation();

            documentInformationDialog.Title = _currentSearchVisualisation.CurrentSearchAggregateItem.Title;
            documentInformationDialog.IsPublic = true;
            documentInformationDialog.Keywords = "";

            AddWatermark(canvas);

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "ANNOTATIONNFORMATION", documentInformationDialog, "buttonOK", DialogButtons.Ok | DialogButtons.Cancel,
                delegate(DialogResult dialogControlResult)
                {
                    if (dialogControlResult == DialogResult.Ok)
                    {
                        _progressControl = new ProgressControl();
                        _progressControl.Status = CultureHelper.GetString(Properties.Resources.ResourceManager, "ANNOTATIONSENDINGTO").Replace("%1", provider);

                        DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "PROGRESS", _progressControl, "buttonCancel", DialogButtons.Cancel,
                            delegate(DialogResult dialogControlResult2)
                            {
                                _progressControl = null;
                            }
                        );

                        _shareTaskId = Guid.NewGuid();

                        _coreLogic.AnnotationShare(_shareTaskId, provider, canvas, documentInformationDialog.Title, documentInformationDialog.IsPublic, documentInformationDialog.Keywords,
                            delegate(bool success, Guid shareTaskCompleteId, Uri providerUrl)
                            {
                                Action a = delegate()
                                {
                                    // Only do the completion operations if this is the current task
                                    if (_shareTaskId == shareTaskCompleteId)
                                    {
                                        if (!success)
                                        {
                                            DialogPanel.ShowInformationBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "ANNOTATIONUNABLETOSEND").Replace("%1", provider), DialogButtons.Ok, null);
                                        }
                                        else
                                        {
                                            ShumbiDiscover.Controls.Dialogs.DocumentComplete documentCompleteDialog = new ShumbiDiscover.Controls.Dialogs.DocumentComplete();

                                            documentCompleteDialog.Url = providerUrl.ToString();

                                            documentCompleteDialog.OpenUrlRequested += new ShumbiDiscover.Controls.Dialogs.DocumentComplete.OpenUrlRequestedEventHandler(OpenUrl);

                                            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "ANNOTATIONSENDCOMPLETE", documentCompleteDialog, "buttonOK", DialogButtons.Ok, null);
                                        }

                                        if (_progressControl != null)
                                        {
                                            DialogPanel.Close(_progressControl, success ? DialogResult.Ok : DialogResult.Cancel);
                                            _progressControl = null;
                                        }

                                        _shareTaskId = Guid.Empty;
                                    }
                                };

#if SILVERLIGHT
                                Dispatcher.BeginInvoke(a);
#else
                                Dispatcher.Invoke(a);
#endif
                            }
                         );
                    }
                }
            );
        }
        #endregion

        #region Search Visualisation Event Handlers
        void SearchVisualisation_ItemSelected(SearchCluster selectedItem)
        {
            InformationPanelRefresh();
            ActionButtonsRefresh();
        }

        void SearchVisualisation_ItemActivated(SearchCluster activatedItem)
        {
            OpenDocumentViewer();
        }
        #endregion

        #region Item Methods
        private void OpenUrl(Uri url)
        {
            try
            {
#if SILVERLIGHT
                if (System.Windows.Browser.HtmlPage.Window.Navigate(url, "_blank") == null)
                {
                    DialogPanel.ShowInformationBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "POPUPBLOCKER"), DialogButtons.Ok, null);
                }
#else
                System.Diagnostics.Process.Start(url.ToString());
#endif
            }
            catch { }
        }
        #endregion

        #region Core Logic Events
        void CoreLogic_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Favourites")
            {
                buttonFavourites.IsEnabled = true;
            }
            else if (e.PropertyName == "History")
            {
                buttonHistory.IsEnabled = true;
            }
            else if (e.PropertyName == "SelectedVisualisation")
            {
                visualisationSelector.Visualisation = _coreLogic.SelectedVisualisation;
            }
            else if (e.PropertyName == "SelectedSearchProviders")
            {
                searchSelector.SearchProviders = _coreLogic.SelectedSearchProviders;
            }
        }

        void CoreLogic_CheckForDiskSpace(AbstractCoreLogic.OperationComplete checkForDiskSpaceComplete)
        {
#if SILVERLIGHT
            DialogPanel.ShowQuestionBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "ISOLATEDSTORAGEINCREASE"), DialogButtons.Yes | DialogButtons.No,
                delegate(DialogResult dialogResult)
                {
                    if (checkForDiskSpaceComplete != null)
                    {
                        checkForDiskSpaceComplete(dialogResult == DialogResult.Yes ? true : false);
                    }
                }
            );
#endif
        }
        #endregion

        #region Searching
        private void SearchVisualise(string query)
        {
            documentViewer.Visibility = Visibility.Collapsed;
            documentViewer.Cleanup();
            annotationCanvas.ClearContent();
            DisplayNewVisualisation(null);

            ShowSearchResultProgressDialog(CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHING") + "...");

            foreach (string searchProviderName in searchSelector.SearchProviders)
            {
                _progressControl.AddItem(searchProviderName, _coreLogic.NumberItemsToRetrieve);
            }

            SearchResultSet searchResultSet = new SearchResultSet();
            searchResultSet.SearchDescription = new SearchDescription();
            searchResultSet.SearchDescription.Query = query;
            searchResultSet.SearchDescription.QueryDate = DateTime.Now;
            searchResultSet.SearchDescription.SearchProviders = searchSelector.SearchProviders;

            _currentSearchVisualisation = new SearchVisualisation();
            _currentSearchVisualisation.Visualisation = visualisationSelector.Visualisation;
            _currentSearchVisualisation.Localise();
            _currentSearchVisualisation.ItemActivated += new SearchVisualisation.ItemActivatedEventHandler(SearchVisualisation_ItemActivated);
            _currentSearchVisualisation.ItemSelected += new SearchVisualisation.ItemSelectedEventHandler(SearchVisualisation_ItemSelected);
            _currentSearchVisualisation.SearchResultSet = searchResultSet;

            searchVisualisationTab.AddSearchVisualisation(_currentSearchVisualisation);

            InformationPanelRefresh();
            ActionButtonsRefresh();

            _gettingProgress = false;
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(200);
            _progressTimer.Tick += new EventHandler(_progressTimer_Tick);
            _progressTimer.Start();

            _coreLogic.SearchStartSession(searchResultSet.SearchDescription.SearchProviders, query,
                delegate(SearchResultSet searchResultSet2)
                {
                    Action a = delegate()
                    {
                        if (_progressTimer != null)
                        {
                            _progressTimer.Stop();
                            _gettingProgress = false;
                        }

                        if (searchResultSet2 != null && _currentSearchVisualisation != null)
                        {
                            _currentSearchVisualisation.SearchResultSet = searchResultSet2;
                            _currentSearchVisualisation.PopulateVisualisation();
                            DialogPanel.Close(_progressControl, DialogResult.Ok);
                        }
                        else
                        {
                            DialogPanel.Close(_progressControl, DialogResult.Cancel);
                        }
                    };
#if SILVERLIGHT
                    Dispatcher.BeginInvoke(a);
#else
                    Dispatcher.Invoke(a);
#endif
                }
             );
        }

        void _progressTimer_Tick(object sender, EventArgs e)
        {
            if (!_gettingProgress)
            {
                if (_currentSearchVisualisation != null)
                {
                    _gettingProgress = true;

                    _coreLogic.SearchGetProgress(delegate(Dictionary<string, int> progress, bool isSearching)
                    {
                        if (_progressControl != null)
                        {
                            if (progress != null)
                            {
                                if (progress.Count > 0)
                                {
                                    if (!isSearching)
                                    {
                                        _progressControl.Status = CultureHelper.GetString(Properties.Resources.ResourceManager, "CLUSTERING") + "...";
                                        foreach (KeyValuePair<string, int> pro in progress)
                                        {
                                            _progressControl.UpdateItem(pro.Key, _coreLogic.NumberItemsToRetrieve);
                                        }
                                    }
                                    else
                                    {
                                        foreach (KeyValuePair<string, int> pro in progress)
                                        {
                                            _progressControl.UpdateItem(pro.Key, pro.Value);
                                        }
                                    }
                                }
                            }
                        }

                        _gettingProgress = false;
                    });
                }
            }
        }

        private void ShowSearchResultProgressDialog(string initialStatus)
        {
            _progressControl = new ProgressControl();
            _progressControl.Status = initialStatus;

            DialogPanel.ShowDialog(Properties.Resources.ResourceManager, "PROGRESS", _progressControl, "buttonCancel", DialogButtons.Cancel,
                delegate(DialogResult dialogControlResult)
                {
                    _progressControl = null;
                    if (dialogControlResult == DialogResult.Cancel)
                    {
                        CleanupSearchVisualisation();
                    }
                }
            );
        }

        private void CleanupSearchVisualisation()
        {
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _gettingProgress = false;
            }

            if (_progressControl != null)
            {
                DialogPanel.Close(_progressControl, DialogResult.Cancel);
            }

            _coreLogic.SearchSessionEnd();

            searchVisualisationTab.RemoveSearchVisualisation(_currentSearchVisualisation);
        }
        #endregion

        #region Factory Registration
        private static void RegisterSearchProviders()
        {
            SearchProviderFactory.Register("Bing", "Bing", "Web", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Bing48.png", UriKind.Relative));

            //SearchProviderFactory.Register("Yahoo", "Yahoo", "Web", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Yahoo48.png", UriKind.Relative));

            SearchProviderFactory.Register("Google", "Google", "Web", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Google48.png", UriKind.Relative));

            SearchProviderFactory.Register("Wikipedia", "Wikipedia", "Web", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Wikipedia48.png", UriKind.Relative));

            SearchProviderFactory.Register("Twitter", "Twitter", "Social", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Twitter48.png", UriKind.Relative));

            SearchProviderFactory.Register("FriendFeed", "FriendFeed", "Social", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/FriendFeed48.png", UriKind.Relative));

            SearchProviderFactory.Register("Flickr", "Flickr", "Pictures", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Flickr48.png", UriKind.Relative));

            SearchProviderFactory.Register("Photobucket", "Photobucket", "Pictures", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Photobucket48.png", UriKind.Relative));

            SearchProviderFactory.Register("Bing Images", "Bing", "Pictures", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Bing48.png", UriKind.Relative));

            SearchProviderFactory.Register("Google Images", "Google", "Pictures", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Google48.png", UriKind.Relative));

            SearchProviderFactory.Register("YouTube", "YouTube", "Videos", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/YouTube48.png", UriKind.Relative));

            SearchProviderFactory.Register("Bing Videos", "Bing", "Videos", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Bing48.png", UriKind.Relative));

            //SearchProviderFactory.Register("Google Videos", "Google", "Videos", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Google48.png", UriKind.Relative));
            
            SearchProviderFactory.Register("Scribd", "Scribd", "Documents", new Uri("/ShumbiDiscover.Core;component/Resources/SearchProviders/Scribd48.png", UriKind.Relative));
        }

        private static void RegisterVisualisations()
        {
            VisualisationFactory.Register("Network", new Uri("/ShumbiDiscover.Core;component/Resources/Visualisations/NetworkVisualisation48.png", UriKind.Relative)
                                                     , new Uri("/ShumbiDiscover.Core;component/Resources/Visualisations/NetworkVisualisation48.png", UriKind.Relative),
                                                        typeof(ShumbiDiscover.Visualisations.NetworkControl));

            VisualisationFactory.Register("TagCloud", new Uri("/ShumbiDiscover.Core;component/Resources/Visualisations/TagCloudVisualisation48.png", UriKind.Relative)
                                                     , new Uri("/ShumbiDiscover.Core;component/Resources/Visualisations/TagCloudVisualisation48.png", UriKind.Relative),
                                                        typeof(ShumbiDiscover.Visualisations.TagCloud));

            VisualisationFactory.Register("TreeMap", new Uri("/ShumbiDiscover.Core;component/Resources/Visualisations/TreeMapVisualisation48.png", UriKind.Relative)
                                                     , new Uri("/ShumbiDiscover.Core;component/Resources/Visualisations/TreeMapVisualisation48.png", UriKind.Relative),
                                                        typeof(ShumbiDiscover.Visualisations.TreeMap));
        }

        private static void RegisterThumbnailGenerators()
        {
            ThumbnailManager.RegisterThumbnailGenerator("http", typeof(WebThumbnailGenerator));
            ThumbnailManager.RegisterThumbnailGenerator("https", typeof(WebThumbnailGenerator));
        }
        #endregion
    }
}
