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
using Obany.UI;
using ShumbiDiscover.Data;
using ShumbiDiscover.Model;

namespace ShumbiDiscover.Controls
{
    /// <summary>
    /// Class to contain all the components of one search with visualisation
    /// </summary>
    public class SearchVisualisation
    {
        #region Delegate
        /// <summary>
        /// Event handler used when an item is activated
        /// </summary>
        /// <param name="activatedItem">The item being activated</param>
        public delegate void ItemActivatedEventHandler(SearchCluster activatedItem);
        /// <summary>
        /// Event handler used when an item is selected
        /// </summary>
        /// <param name="selectedItem">The item being selected</param>
        public delegate void ItemSelectedEventHandler(SearchCluster selectedItem);
        #endregion

        #region Fields
        private SearchResultSet _searchResultSet;

        private bool _visualisationInitialised;
        private IVisualisation _visualisation;

        private SearchCluster _currentCluster;
        private bool _isClusterOpen;
        private SearchAggregateItem _currentSearchAggregateItem;
        private DocumentViewerDisplayMode _documentViewDisplayMode;

        private Dictionary<Guid, SearchAggregateItem> _searchResultsDictionary;

        private bool _hidHooked;
        #endregion

        #region Events
        /// <summary>
        /// Event handler called when an item is activated
        /// </summary>
        public event ItemActivatedEventHandler ItemActivated;
        /// <summary>
        /// Event handler called when an item is selected
        /// </summary>
        public event ItemSelectedEventHandler ItemSelected;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public SearchVisualisation()
        {
            _documentViewDisplayMode = DocumentViewerDisplayMode.Carousel;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the search result set
        /// </summary>
        public SearchResultSet SearchResultSet
        {
            get
            {
                return (_searchResultSet);
            }
            set
            {
                _searchResultSet = value;
                _searchResultsDictionary = new Dictionary<Guid, SearchAggregateItem>();

                if (_searchResultSet != null)
                {
                    if (_searchResultSet.SearchAggregateResults != null)
                    {
                        foreach (SearchAggregateItem item in _searchResultSet.SearchAggregateResults)
                        {
                            _searchResultsDictionary.Add(item.Id, item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the visualisation
        /// </summary>
        public string Visualisation
        {
            set
            {
                _visualisationInitialised = false;
                _currentCluster = null;

                _visualisation = VisualisationFactory.CreateInstance(value);

                if (_visualisation != null)
                {
                    _visualisation.ItemActivated += new ShumbiDiscover.Model.ItemActivatedEventHandler(_visualisation_ItemActivated);
                    _visualisation.ItemSelected += new ShumbiDiscover.Model.ItemSelectedEventHandler(_visualisation_ItemSelected);

                    FrameworkElement fe = _visualisation as FrameworkElement;
                    if (fe != null)
                    {
                        fe.LayoutUpdated += new EventHandler(_visualisation_LayoutUpdated);
                    }
                }
            }
            get
            {
                return (_visualisation == null ? "" : _visualisation.VisualisationName);
            }
        }

        void _visualisation_LayoutUpdated(object sender, System.EventArgs e)
        {
            if (!_visualisationInitialised)
            {
                _visualisationInitialised = true;

                if (_searchResultSet != null)
                {
                    if (_searchResultSet.SearchClusters != null)
                    {
                        PopulateVisualisation();
                    }
                }
            }
        }

        /// <summary>
        /// Get the visualisation control
        /// </summary>
        public FrameworkElement VisualisationControl
        {
            get
            {
                return (_visualisation as FrameworkElement);
            }
        }

        /// <summary>
        /// Get or set the search result dictionary
        /// </summary>
        public Dictionary<Guid, SearchAggregateItem> SearchResultsDictionary
        {
            get
            {
                return (_searchResultsDictionary);
            }
        }

        /// <summary>
        /// Get the current cluster
        /// </summary>
        public SearchCluster CurrentCluster
        {
            get
            {
                return (_currentCluster);
            }
            set
            {
                _currentCluster = value;
            }
        }

        /// <summary>
        /// Get the current search result item
        /// </summary>
        public SearchAggregateItem CurrentSearchAggregateItem
        {
            get
            {
                return (_currentSearchAggregateItem);
            }
            set
            {
                _currentSearchAggregateItem = value;
            }
        }

        /// <summary>
        /// Is the cluster open
        /// </summary>
        public bool IsClusterOpen
        {
            get
            {
                return (_isClusterOpen);
            }
            set
            {
                _isClusterOpen = value;
            }
        }

        /// <summary>
        /// Get or set the document viewer display mode
        /// </summary>
        public DocumentViewerDisplayMode DocumentViewerDisplayMode
        {
            get
            {
                return (_documentViewDisplayMode);
            }
            set
            {
                _documentViewDisplayMode = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Localise the control
        /// </summary>
        public void Localise()
        {
            ILocalisable localisable = _visualisation as ILocalisable;
            if (localisable != null)
            {
                localisable.Localise();
            }
        }

        /// <summary>
        /// Populate the visualisation
        /// </summary>
        public void PopulateVisualisation()
        {
            if (_visualisation != null)
            {
                _visualisation.ItemsRoot = CreateTree();
            }
        }

        /// <summary>
        /// Change the state of the visualisation
        /// </summary>
        /// <param name="isVisible">Is it visible</param>
        public void VisualisationStateChanged(bool isVisible)
        {
            if (isVisible)
            {
                if (!_hidHooked)
                {
                    _visualisation.HookHidEvents(true);
                    _hidHooked = true;
                }
            }
            else
            {
                if (_hidHooked)
                {
                    _visualisation.HookHidEvents(false);
                    _hidHooked = false;
                }
            }
        }
        #endregion


        #region Private Methods
        private TreeData CreateTree()
        {
            SearchCluster rootCluster = new SearchCluster();
            rootCluster.Title = _searchResultSet.SearchDescription.Query;
            rootCluster.SearchClusters = _searchResultSet.SearchClusters;
            rootCluster.Score = 1;

            TreeData root = new TreeData(rootCluster.Title, rootCluster.Score, rootCluster);

            BuildTree(root, rootCluster.SearchClusters);

            return(root);
        }

        private void BuildTree(TreeData root, List<SearchCluster> clusters)
        {
            if (clusters != null)
            {
                foreach (SearchCluster cluster in clusters)
                {
                    TreeData treeData = new TreeData(cluster.Title, cluster.Score, cluster);

                    root.Add(treeData);

                    BuildTree(treeData, cluster.SearchClusters);
                }
            }
        }
        #endregion

        #region Control Events
        void _visualisation_ItemSelected(object item)
        {
            if (ItemSelected != null)
            {
                _currentCluster = (item as TreeData).Tag as SearchCluster;
                ItemSelected(_currentCluster);
            }
        }

        void _visualisation_ItemActivated(object item)
        {
            if (ItemActivated != null)
            {
                _currentCluster = (item as TreeData).Tag as SearchCluster;
                ItemActivated(_currentCluster);
            }
        }
        #endregion

    }
}
