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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Obany.Core;
using Obany.UI.Controls;
using Obany.UI.Controls.EventArgs;
using ShumbiDiscover.Data;
using ShumbiDiscover.Model;
using System.Collections.ObjectModel;

namespace ShumbiDiscover.Controls
{
    /// <summary>
    /// Class for display document viewer
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class DocumentViewer : UserControl
    {
        #region Delegates
        /// <summary>
        /// Event handler called when item selection changes
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event args</param>
        public delegate void SelectionChangedEventHandler(object sender, EventArgs.SelectionChangedEventArgs e);

        /// <summary>
        /// Event handler for when a url open is required
        /// </summary>
        /// <param name="url">The url to open</param>
        public delegate void OpenUrlRequestedEventHandler(Uri url);

        /// <summary>
        /// Event handler for when the display mode changes
        /// </summary>
        /// <param name="displayMode">The display mode</param>
        public delegate void DisplayModeChangedEventHandler(DocumentViewerDisplayMode displayMode);
        #endregion

        #region Fields
        private DocumentViewerDisplayMode _displayMode;
        private bool _updatingDocumentList;
        private bool _updatingCarousel;
        private bool _updatingScrollNavigator;
        #endregion

        #region Events
        /// <summary>
        /// Event called when selection changes
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged;

        /// <summary>
        /// Event call when a url open is requested
        /// </summary>
        public event OpenUrlRequestedEventHandler OpenUrlRequested;

        /// <summary>
        /// Event called when the display mode changes
        /// </summary>
        public event DisplayModeChangedEventHandler DisplayModeChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public DocumentViewer()
        {
            InitializeComponent();

            buttonCarousel.IsChecked = true;
            gridList.Visibility = Visibility.Collapsed;

            listBoxSearchItems.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(listBoxSearchItems_SelectionChanged);
            carousel.SelectedIndexChanged += new Obany.UI.Controls.CarouselPanel.SelectedIndexChangedEventHandler(carousel_SelectedIndexChanged);
            carousel.SelectedIndexActivated += new CarouselPanel.SelectedIndexActivatedEventHandler(carousel_SelectedIndexActivated);

            this.SizeChanged += new SizeChangedEventHandler(DocumentViewer_SizeChanged);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the selected item
        /// </summary>
        public SearchAggregateItem SelectedItem
        {
            get
            {
                return (listBoxSearchItems.SelectedItem as SearchAggregateItem);
            }
        }

        /// <summary>
        /// Get the display mode
        /// </summary>
        public DocumentViewerDisplayMode DisplayMode
        {
            get
            {
                return (_displayMode);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the flicks allowed flag
        /// </summary>
        /// <param name="allowed"></param>
        public void SetFlicksAllowed(bool allowed)
        {
            carousel.SetFlicksAllowed(allowed);
        }
        
        /// <summary>
        /// Cleanup the document viewer
        /// </summary>
        public void Cleanup()
        {
            gridBuffer.Children.Clear();
            listBoxSearchItems.ItemsSource = null;
            carousel.ItemsSource = null;

            documentViewerTitle.Text = "";

            carousel.HookHidEvents(false);
        }

        /// <summary>
        /// Populate the document viewer
        /// </summary>
        /// <param name="cluster">The cluster</param>
        /// <param name="searchResultDictionary">The document from the search results</param>
        /// <param name="displayMode">The display mode for the document viewer</param>
        /// <param name="kindFilters">Kind filter to decide which results to include</param>
        /// <param name="providerFilters">Provider filter to decide which results to include</param>
        public void Populate(SearchCluster cluster, Dictionary<Guid, SearchAggregateItem> searchResultDictionary, DocumentViewerDisplayMode displayMode, List<string> kindFilters, List<string> providerFilters)
        {
            carousel.HookHidEvents(false);
            carousel.HookHidEvents(true);

            Action openDocumentViewer = delegate()
            {
                listBoxSearchItems.ItemsSource = null;
                carousel.ItemsSource = null;

                documentViewerTitle.Text = cluster.Title;

                ShumbiDiscover.Controls.Converters.SearchAggregateItemRankConverter.ProviderFilters = providerFilters;
                ShumbiDiscover.Controls.Converters.SearchAggregateItemProvidersConverter.ProviderFilters = providerFilters;

                List<SearchAggregateItem> items = new List<SearchAggregateItem>();

                PopulateDocumentViewerFromCluster(cluster, searchResultDictionary, items, kindFilters, providerFilters);

                items.Sort(new SearchAggregateItemComparer(providerFilters));

                carousel.ItemsSource = items;
                listBoxSearchItems.ItemsSource = items;

                scrollNavigator.Minimum = 0;
                scrollNavigator.Maximum = items.Count - 1;

                if (listBoxSearchItems.Items.Count > 0)
                {
                    listBoxSearchItems.SelectedIndex = 0;
                }

                _displayMode = displayMode;
                buttonCarousel.IsChecked = _displayMode == DocumentViewerDisplayMode.Carousel;
                buttonList.IsChecked = _displayMode == DocumentViewerDisplayMode.List;
                gridCarousel.Visibility = _displayMode == DocumentViewerDisplayMode.Carousel ? Visibility.Visible : Visibility.Collapsed;
                gridList.Visibility = _displayMode == DocumentViewerDisplayMode.List ? Visibility.Visible : Visibility.Collapsed;

                List<SearchAggregateItem> missingThumbs = new List<SearchAggregateItem>();

                foreach (SearchAggregateItem searchAggregateItem in items)
                {
                    if (string.IsNullOrEmpty(searchAggregateItem.ThumbnailUrl))
                    {
                        missingThumbs.Add(searchAggregateItem);
                        ThumbnailManager.Instance.GetThumbnail(searchAggregateItem.OpenUrl, 300, 300, true, searchAggregateItem, new ThumbnailCompletedEventHandler(ThumbnailCompleted));
                    }
                }
                foreach (SearchAggregateItem searchAggregateItem in missingThumbs)
                {
                    ThumbnailManager.Instance.GetThumbnail(searchAggregateItem.OpenUrl, 300, 300, false, searchAggregateItem, new ThumbnailCompletedEventHandler(ThumbnailCompleted));
                }
            };

            Dispatcher.BeginInvoke(openDocumentViewer);
        }
        #endregion

        private void PopulateDocumentViewerFromCluster(SearchCluster cluster, Dictionary<Guid, SearchAggregateItem> searchResultDictionary, List<SearchAggregateItem> items, List<string> kindFilters, List<string> providerFilters)
        {
            if (cluster.SearchClusters != null)
            {
                foreach (SearchCluster subCluster in cluster.SearchClusters)
                {
                    PopulateDocumentViewerFromCluster(subCluster, searchResultDictionary, items, kindFilters, providerFilters);
                }
            }

            if (cluster.SearchResultIds != null)
            {
                foreach (Guid searchResultId in cluster.SearchResultIds)
                {
                    if (searchResultDictionary.ContainsKey(searchResultId))
                    {
                        SearchAggregateItem searchResultItem = searchResultDictionary[searchResultId];

                        // First skip any duplicates
                        if (!items.Contains(searchResultItem))
                        {
                            if (kindFilters.Contains(searchResultItem.Kind))
                            {
                                bool found = false;

                                for (int i = 0; i < searchResultItem.ProviderRanks.Count && !found; i++)
                                {
                                    if(providerFilters.Contains(searchResultItem.ProviderRanks[i].Provider))
                                    {
                                        items.Add(searchResultItem);
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ThumbnailCompleted(string url, bool isPreview, object userState, bool success)
        {
            if (success)
            {
                SearchAggregateItem searchResultItem = userState as SearchAggregateItem;

                if (searchResultItem != null)
                {
                    Action a = delegate()
                    {
                        if (!string.IsNullOrEmpty(url))
                        {
#if SILVERLIGHT
                            // If this is the preview then just set the image url
                            // if it is not then load the image in the background until it is ready
                            // this way there will be no flicker when the images are switched
                            // But still do a background load of the image for previews as this will 
                            // improve display times for images that have yet to be viewed in a control
                            if (isPreview)
                            {
                                // By assigning the new url source it will trigger the notify property changed
                                // and propogate to anything displaying it
                                searchResultItem.ThumbnailUrl = url;
                            }

                            // Load the new url into an image control, only when the image
                            // has been successfully loaded do we update anything else
                            // by triggering the property changed

                            Image image = new Image();

                            System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();

                            gridBuffer.Children.Add(image);
                            image.Width = 300;
                            image.Height = 300;

                            bitmapImage.ImageOpened += delegate(object sender, RoutedEventArgs e)
                            {
                                if (!isPreview)
                                {
                                    // By assigning the new url source it will trigger the notify property changed
                                    // and propogate to anything displaying it
                                    searchResultItem.ThumbnailUrl = url;
                                }

                                if (gridBuffer.Children.Contains(image))
                                {
                                    gridBuffer.Children.Remove(image);
                                }
                            };
                            bitmapImage.UriSource = new Uri(url);

                            image.Source = bitmapImage;
#else
                            // By assigning the new url source it will trigger the notify property changed
                            // and propogate to anything displaying it
                            searchResultItem.ThumbnailUrl = url;
#endif
                        }
                    };
                    Dispatcher.BeginInvoke(a);
                }
            }
        }

        #region Control Event Handlers
        void listBoxSearchItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatingDocumentList)
            {
                UpdateCarousel(listBoxSearchItems.SelectedIndex);
                UpdateScrollNavigator(listBoxSearchItems.SelectedIndex);

                SelectedItemChanged();
            }
        }

        void UpdateCarousel(int currentIndex)
        {
            _updatingCarousel = true;

            carousel.SelectedIndex = currentIndex;

            _updatingCarousel = false;
        }

        void UpdateDocumentViewListBox(int currentIndex)
        {
            _updatingDocumentList = true;

            listBoxSearchItems.SelectedIndex = currentIndex;

            _updatingDocumentList = false;
        }


        void carousel_SelectedIndexChanged(object sender, Obany.UI.Controls.EventArgs.SelectedIndexChangedEventArgs e)
        {
            if (!_updatingCarousel)
            {
                UpdateDocumentViewListBox(e.NewIndex);
                UpdateScrollNavigator(e.NewIndex);

                SelectedItemChanged();
            }
        }

        void carousel_SelectedIndexActivated(object sender, System.EventArgs e)
        {
            SearchAggregateItem item = carousel.SelectedItem as SearchAggregateItem;
            if (item != null)
            {
                if (OpenUrlRequested != null)
                {
                    OpenUrlRequested(new Uri(item.OpenUrl));
                }
            }
        }

        private void SelectedItemChanged()
        {
            SearchAggregateItem item = carousel.SelectedItem as SearchAggregateItem;

            if (SelectionChanged != null)
            {
                SelectionChanged(this, new EventArgs.SelectionChangedEventArgs(item));
            }

            if (item != null)
            {
                textItemTitle.Text = ShumbiDiscover.Controls.Converters.SearchAggregateItemTitleConverter.Convert(item.Title);
                textItemDescription.Text = ShumbiDiscover.Controls.Converters.SearchAggregateItemDescriptionConverter.Convert(item.Description, 0);
                textItemAddress.Text = ShumbiDiscover.Controls.Converters.SearchAggregateItemUrlConverter.Convert(item.OpenUrl);
                textItemRank.Text = ShumbiDiscover.Controls.Converters.SearchAggregateItemRankConverter.Convert(item.ProviderRanks);
                textItemSearchProviders.Text = ShumbiDiscover.Controls.Converters.SearchAggregateItemProvidersConverter.Convert(item.ProviderRanks);
            }
            else
            {
                textItemTitle.Text = "";
                textItemDescription.Text = "";
                textItemAddress.Text = "";
                textItemRank.Text = "";
                textItemSearchProviders.Text = "";
            }
        }

        void UpdateScrollNavigator(int currentIndex)
        {
            if (!_updatingScrollNavigator)
            {
                _updatingScrollNavigator = true;

                scrollNavigator.Value = currentIndex;

                _updatingScrollNavigator = false;
            }
        }

        private void scrollNavigator_NavigatorValueChanged(object sender, NavigationValueChangedEventArgs e)
        {
            if (!_updatingScrollNavigator)
            {
                UpdateDocumentViewListBox(e.NewValue);
                UpdateCarousel(e.NewValue);

                SelectedItemChanged();
            }
        }

        private void buttonCarousel_Click(object sender, RoutedEventArgs e)
        {
            buttonCarousel.IsChecked = true;
            buttonList.IsChecked = false;
            gridCarousel.Visibility = Visibility.Visible;
            gridList.Visibility = Visibility.Collapsed;
            _displayMode = DocumentViewerDisplayMode.Carousel;
            if (DisplayModeChanged != null)
            {
                DisplayModeChanged(_displayMode);
            }
        }

        private void buttonList_Click(object sender, RoutedEventArgs e)
        {
            buttonCarousel.IsChecked = false;
            buttonList.IsChecked = true;
            gridCarousel.Visibility = Visibility.Collapsed;
            gridList.Visibility = Visibility.Visible;
            _displayMode = DocumentViewerDisplayMode.List;
            if (DisplayModeChanged != null)
            {
                DisplayModeChanged(_displayMode);
            }
        }

        private void textItemAddress_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SearchAggregateItem item = carousel.SelectedItem as SearchAggregateItem;
            if (item != null)
            {
                if (OpenUrlRequested != null)
                {
                    OpenUrlRequested(new Uri(item.OpenUrl));
                }
            }
        }

        void DocumentViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualHeight < 350)
            {
                rowDefinitionInformation.Height = new GridLength(0);
            }
            else
            {
                rowDefinitionInformation.Height = new GridLength(160);
            }
        }

        private void imageThumbnail_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void listBoxSearchItems_MouseWheel(object sender, MouseWheelEventArgs e)
        {
#if SILVERLIGHT
            int newpos = listBoxSearchItems.SelectedIndex + (e.Delta > 0 ? -1 : 1);

            if (newpos >= 0 && newpos < listBoxSearchItems.Items.Count)
            {
                listBoxSearchItems.SelectedIndex = newpos;
            }
            e.Handled = true;
#endif
        }
        #endregion

        #region ILocalisable Methods

        /// <summary>
        /// Localise the control
        /// </summary>
        public void Localise()
        {
            //labelHeaderIcon.Text = "";
            //labelHeaderTitle.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERTITLE");
            //labelHeaderDescription.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERDESCRIPTION");
            //labelHeaderAddress.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERADDRESS");
            //labelHeaderRank.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERRANK");
            labelItemTitle.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERTITLE");
            labelItemDescription.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERDESCRIPTION");
            labelItemAddress.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERADDRESS");
            labelItemRank.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERRANK");
            labelItemSearchProviders.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERSEARCHENGINES");
            labelCarousel.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERCAROUSEL");
            labelList.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTVIEWERLIST");
        }
        #endregion

    }
}
