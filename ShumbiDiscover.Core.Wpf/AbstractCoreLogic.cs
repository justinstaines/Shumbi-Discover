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
using System.Linq;
using System.Text;
using ShumbiDiscover.Data;
using System.Collections.ObjectModel;
using Obany.Core;
using System.ComponentModel;
using System.Net;

namespace ShumbiDiscover.Core
{
    /// <summary>
    /// Class to manage the business logic
    /// </summary>
    abstract class AbstractCoreLogic : INotifyPropertyChanged
    {
        #region Delegates
        public delegate void OperationComplete(bool success);
        public delegate void AnnotationRenderWebPageComplete(bool success, Guid renderTaskId, byte[] imageData, int imageWidth, int imageHeight, string imageMimeType, string renderDataId);
        public delegate void AnnotationShareComplete(bool success, Guid shareTaskId, Uri providerUrl);
        public delegate void AnnotationSaveComplete(bool success, Guid saveTaskId);
        public delegate void CheckForDiskSpaceComplete(OperationComplete checkForDiskSpaceComplete);
        public delegate void SearchProgressDelegate(Dictionary<string, int> progress, bool isSearching);
        public delegate void SearchResultSetDelegate(SearchResultSet searchResultSet);
        #endregion

        #region Fields
        protected int _numItemsToRetrieve;
        protected List<SearchDescription> _searchHistory;
        protected FavouriteFolder _favourites;
        protected ObservableCollection<ApiCredential> _apiCredentials;
        protected List<string> _selectedSearchProviders;
        protected string _selectedVisualisation;
        protected int _historyItemsToKeep;
        protected int _thumbnailCacheSize;
        #endregion

        #region Events
        public event CheckForDiskSpaceComplete CheckForDiskSpace;
        #endregion

        #region INotifyPropertyChanged Events
        /// <summary>
        /// Event that is fired when a property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public AbstractCoreLogic()
        {
            _numItemsToRetrieve = 100;

            _searchHistory = new List<SearchDescription>();
            _favourites = new FavouriteFolder();
            _apiCredentials = new ObservableCollection<ApiCredential>();
            _selectedSearchProviders = new List<string>();
            _selectedVisualisation = "Network";

            _historyItemsToKeep = 50;
            _thumbnailCacheSize = 50;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the number of items to retrieve
        /// </summary>
        public int NumberItemsToRetrieve
        {
            get
            {
                return (_numItemsToRetrieve);
            }
        }
        
        /// <summary>
        /// Get the favourites
        /// </summary>
        public FavouriteFolder Favourites
        {
            get
            {
                return (_favourites);
            }
        }

        /// <summary>
        /// Get the history
        /// </summary>
        public List<SearchDescription> History
        {
            get
            {
                return (_searchHistory);
            }
        }

        /// <summary>
        /// Get the api credentials
        /// </summary>
        public ObservableCollection<ApiCredential> ApiCredentials
        {
            get
            {
                return (_apiCredentials);
            }
        }

        /// <summary>
        /// Get or set the selected search providers
        /// </summary>
        public List<string> SelectedSearchProviders
        {
            get
            {
                return (_selectedSearchProviders);
            }
            set
            {
                _selectedSearchProviders = value;
            }
        }

        /// <summary>
        /// Get or set the selected visualisation
        /// </summary>
        public string SelectedVisualisation
        {
            get
            {
                return (_selectedVisualisation);
            }
            set
            {
                _selectedVisualisation = value;
            }
        }

        /// <summary>
        /// Get or set the number of history items to keep
        /// </summary>
        public int HistoryItemsToKeep
        {
            get
            {
                return (_historyItemsToKeep);
            }
            set
            {
                _historyItemsToKeep = value;
            }
        }

        /// <summary>
        /// Get or set the thumbnail cache size
        /// </summary>
        public int ThumbnailCacheSize
        {
            get
            {
                return (_thumbnailCacheSize);
            }
            set
            {
                _thumbnailCacheSize = value;
            }
        }

        #endregion

        #region Abstract Properties
        /// <summary>
        /// Get the thumbnail cache location
        /// </summary>
        public abstract string ThumbnailCacheLocation { get; }
        #endregion

        #region Public Methods
        public bool ApiCredentialsExist(string provider)
        {
            bool contains = false;

            for (int i = 0; i < _apiCredentials.Count && !contains; i++)
            {
                if (_apiCredentials[i].Provider == provider)
                {
                    contains = true;
                }
            }

            return (contains);
        }

        public ApiCredential ApiCredentialsGet(string provider)
        {
            ApiCredential apic = null;

            for (int i = 0; i < _apiCredentials.Count && apic == null; i++)
            {
                if (_apiCredentials[i].Provider == provider)
                {
                    apic = _apiCredentials[i];
                }
            }

            return (apic);
        }

        public void ApiCredentialsAdd(ApiCredential apic)
        {
            ApiCredentialsDelete(apic.Provider);

            _apiCredentials.Add(apic);
        }

        public void ApiCredentialsDelete(string provider)
        {
            bool found = false;

            for (int i = 0; i < _apiCredentials.Count && !found; i++)
            {
                if (_apiCredentials[i].Provider == provider)
                {
                    found = true;
                    _apiCredentials.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Account
        public abstract void AccountStartSession(string applicationName, OperationComplete accountStartSessionComplete);
        public virtual void AccountEndSession()
        {
            _favourites = new FavouriteFolder();
            _searchHistory = new List<SearchDescription>();
            _apiCredentials = new ObservableCollection<ApiCredential>();
        }
        public abstract void AccountSetCulture(string culture);
        #endregion

        #region Configuration
        public abstract void ConfigurationSave();
        #endregion

        #region Favourites
        public virtual void FavouritesAdd(Favourite favourite)
        {
            _favourites.Children.Add(favourite);
        }
        public abstract void FavouritesSave();
        #endregion
        
        #region History
        public virtual void HistoryDelete(SearchDescription searchDescription)
        {
            _searchHistory.Remove(searchDescription);
            SearchResultSetDelete(searchDescription.Id);
        }

        public virtual void HistoryClear()
        {
            while (_searchHistory.Count > 0)
            {
                SearchDescription oldestSearch = _searchHistory[_searchHistory.Count - 1];
                _searchHistory.Remove(oldestSearch);
            }
            SearchResultSetsClear();
        }

        public virtual void HistoryCleanup()
        {
            while (_searchHistory.Count > _historyItemsToKeep)
            {
                SearchDescription oldestSearch = _searchHistory[_searchHistory.Count - 1];
                _searchHistory.Remove(oldestSearch);
                SearchResultSetDelete(oldestSearch.Id);
            }
        }

        public abstract long HistorySpaceUsed();
        #endregion

        #region Thumbnail Cache
        public abstract void ThumbnailCacheCleanup(long additionalSize);
        public abstract void ThumbnailCacheClear();
        public abstract long ThumbnailCacheSpaceUsed();
        #endregion

        #region Api Credentials
        public abstract void ApiCredentialsSet(string provider, string apiCredentials1, string apiCredentials2, string apiCredentials3, string apiCredentials4, OperationComplete apiCredentialsSetComplete);
        public abstract void ApiCredentialsDelete(string provider, OperationComplete apiCredentialsDeleteComplete);
        #endregion

        #region Search Result Sets
        public abstract void SearchResultSetLoad(SearchDescription searchDescription, SearchResultSetDelegate searchResultSetLoadComplete);
        public abstract void SearchResultSetDelete(Guid searchResultSetId);
        public abstract void SearchResultSetsClear();
        #endregion

        #region Search
        public abstract void SearchStartSession(List<string> searchProviders, string query, SearchResultSetDelegate searchStartSessionComplete);
        public abstract void SearchSessionEnd();
        public abstract void SearchGetProgress(SearchProgressDelegate searchGetProgressComplete);
        #endregion

        #region Annotation
        public abstract void AnnotationSave(Guid saveTaskId, System.IO.Stream saveStream, Obany.Render.Objects.Canvas canvas, string mimeType, AnnotationSaveComplete annotationSaveComplete);
        public abstract void AnnotationRenderWebPage(Guid renderTaskId, string renderUrl, AnnotationRenderWebPageComplete annotationRenderWebPageComplete);
        public abstract void AnnotationCleanup();
        public abstract void AnnotationShare(Guid shareTaskId, string provider, Obany.Render.Objects.Canvas canvas, string title, bool isPublic, string keywords, AnnotationShareComplete annotationShareComplete);
        #endregion

        #region Invoke Methods
        public void InvokeCheckForDiskSpace(OperationComplete checkForDiskSpaceComplete)
        {
            if (CheckForDiskSpace != null)
            {
                CheckForDiskSpace(checkForDiskSpaceComplete);
            }
        }

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Configuration Storage
        protected void ConfigurationDeserialise(byte[] data)
        {
            if (data != null)
            {
                Configuration config = Obany.Language.Xml.XmlHelper.BinaryDeserialize<Configuration>(data);

                if (config != null)
                {
                    _selectedSearchProviders = config.SearchProviders;
                    _selectedVisualisation = config.Visualisation;
                    
                    _apiCredentials.Clear();
                    foreach (ApiCredential apic in config.ApiCredentials)
                    {
                        _apiCredentials.Add(apic);
                    }
                    _historyItemsToKeep = Math.Max(1, config.HistoryItemsToKeep);
                    _thumbnailCacheSize = Math.Max(1, config.ThumbnailCacheSize);

                    InvokePropertyChanged("SelectedSearchProviders");
                    InvokePropertyChanged("SelectedVisualisation");

                    CultureHelper.CurrentCulture = config.Culture;
                }
            }
        }

        protected byte[] ConfigurationSerialise()
        {
            byte[] data = null;

            Configuration configuration = new Configuration();
            configuration.SearchProviders = _selectedSearchProviders;
            configuration.Visualisation = _selectedVisualisation;
            configuration.ApiCredentials = new List<ApiCredential>();
            configuration.Culture = CultureHelper.CurrentCulture;
            configuration.ThumbnailCacheSize = _thumbnailCacheSize;
            configuration.HistoryItemsToKeep = _historyItemsToKeep;

            foreach (ApiCredential apic in _apiCredentials)
            {
                configuration.ApiCredentials.Add(apic);
            }

            data = Obany.Language.Xml.XmlHelper.BinarySerialize(configuration);

            return (data);
        }
        #endregion

        #region History Storage
        protected void HistoryDeserialise(byte[] data)
        {
            if (data != null)
            {
                History history = Obany.Language.Xml.XmlHelper.BinaryDeserialize<History>(data);

                if (history != null)
                {
                    _searchHistory.Clear();
                    _searchHistory.AddRange(history.Entries);
                }
            }
        }

        protected byte[] HistorySerialise()
        {
            byte[] data = null;

            if (_searchHistory != null)
            {
                History history = new History();
                history.Entries = _searchHistory;
                data = Obany.Language.Xml.XmlHelper.BinarySerialize(history);
            }

            return (data);
        }
        #endregion

        #region Favourites Storage
        protected void FavouritesDeserialise(byte[] data)
        {
            if (data != null)
            {
                FavouriteFolder newFavourites = Obany.Language.Xml.XmlHelper.BinaryDeserialize<FavouriteFolder>(data);

                if (newFavourites != null)
                {
                    if (newFavourites.Children != null)
                    {
                        if (newFavourites.Children.Count > 0)
                        {
                            _favourites = newFavourites;
                        }
                    }
                }
            }
        }

        protected byte[] FavouritesSerialise()
        {
            byte[] data = null;

            if (_favourites != null)
            {
                data = Obany.Language.Xml.XmlHelper.BinarySerialize(_favourites);
            }

            return (data);
        }
        #endregion
    }
}
