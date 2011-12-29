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
using System.Threading;
using Obany.Provider.Model;
using System.IO;

namespace ShumbiDiscover.Core
{
    /// <summary>
    /// Class to manage the business logic
    /// </summary>
    class CoreLogic : AbstractCoreLogic
    {
        #region Fields
        private string _configurationFolder;
        private string _thumbnailCacheFolder;

        private SearchClusterTask _searchClusterTask;
        private byte[] _renderWebPage;

        private long _thumbnailCacheSpaceUsed;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public CoreLogic()
        {
        }
        #endregion

        #region Abstract Properties
        /// <summary>
        /// Get the thumbnail cache location
        /// </summary>
        public override string ThumbnailCacheLocation 
        {
            get
            {
                return (_thumbnailCacheFolder);
            }
        }
        #endregion

        #region Account
        public override void AccountStartSession(string applicationName, OperationComplete accountStartSessionComplete)
        {
            _configurationFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName);

            UserStorageInitialiseLocal();

            ConfigurationLoadLocal();

            FavouritesLoadLocal();

            HistoryLoadLocal();

            if (accountStartSessionComplete != null)
            {
                accountStartSessionComplete(true);
            }
        }

        public override void AccountEndSession()
        {
            base.AccountEndSession();
        }

        public override void AccountSetCulture(string culture)
        {
        }
        #endregion

        #region Configuration
        public override void ConfigurationSave()
        {
            ConfigurationSaveLocal();
        }
        #endregion

        #region Favourites
        public override void FavouritesAdd(Favourite favourite)
        {
            base.FavouritesAdd(favourite);

            FavouritesSaveLocal();
        }

        public override void FavouritesSave()
        {
            FavouritesSaveLocal();
        }
        #endregion

        #region Api Credentials
        public override void ApiCredentialsSet(string provider, string apiCredentials1, string apiCredentials2, string apiCredentials3, string apiCredentials4, OperationComplete apiCredentialsSetComplete)
        {
            ApiCredential apic = new ApiCredential();

            apic.Provider = provider;
            apic.ApiCredentials1 = Obany.Cryptography.SecureEncryption.EncryptData(apiCredentials1);
            apic.ApiCredentials2 = Obany.Cryptography.SecureEncryption.EncryptData(apiCredentials2);
            apic.ApiCredentials3 = Obany.Cryptography.SecureEncryption.EncryptData(apiCredentials3);
            apic.ApiCredentials4 = Obany.Cryptography.SecureEncryption.EncryptData(apiCredentials4);

            base.ApiCredentialsAdd(apic);

            ConfigurationSave();

            if (apiCredentialsSetComplete != null)
            {
                apiCredentialsSetComplete(true);
            }
        }

        public override void ApiCredentialsDelete(string provider, OperationComplete apiCredentialsDeleteComplete)
        {
            base.ApiCredentialsDelete(provider);
            ConfigurationSave();
            if (apiCredentialsDeleteComplete != null)
            {
                apiCredentialsDeleteComplete(true);
            }
        }
        #endregion

        #region Search Result Sets
        public override void SearchResultSetLoad(SearchDescription searchDescription, SearchResultSetDelegate searchResultSetLoadComplete)
        {
            byte[] localSearchResultSetData = SearchResultSetLoadLocal(searchDescription.Id);

            if (localSearchResultSetData != null)
            {
                SearchResultSet searchResultSet = null;

                try
                {
                    searchResultSet = Obany.Language.Xml.XmlHelper.BinaryDeserialize<SearchResultSet>(localSearchResultSetData);
                }
                catch
                {
                }

                if (searchResultSetLoadComplete != null)
                {
                    searchResultSetLoadComplete(searchResultSet);
                }
            }
        }

        public override void SearchResultSetDelete(Guid searchResultSetId)
        {
            SearchResultSetDeleteLocal(searchResultSetId);
            HistorySaveLocal();
        }

        public override void SearchResultSetsClear()
        {
            SearchResultSetsClearLocal();
            HistorySaveLocal();
        }
        #endregion

        #region Search
        public override void SearchStartSession(List<string> searchProviders, string query, SearchResultSetDelegate searchStartSessionComplete)
        {
            _searchClusterTask = new SearchClusterTask();

            ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    SearchResultSet searchResultSet = new SearchResultSet();
                    searchResultSet.SearchDescription = new SearchDescription();
                    searchResultSet.SearchDescription.Id = Guid.NewGuid();
                    searchResultSet.SearchDescription.Query = query;
                    searchResultSet.SearchDescription.QueryDate = DateTime.Now.ToUniversalTime();
                    searchResultSet.SearchDescription.SearchProviders = searchProviders;

                    searchResultSet.SearchAggregateResults = _searchClusterTask.Search(searchProviders, query, _numItemsToRetrieve, CultureHelper.CurrentCulture);

                    if (searchResultSet.SearchAggregateResults.Count > 0)
                    {
                        if (!_searchClusterTask.Aborted)
                        {
                            searchResultSet.SearchClusters = _searchClusterTask.Cluster(query, CultureHelper.CurrentCulture);
                        }

                        if (!_searchClusterTask.Aborted)
                        {
                            try
                            {
                                byte[] packedData = Obany.Language.Xml.XmlHelper.BinarySerialize(searchResultSet);

                                SearchResultSetSaveLocal(searchResultSet.SearchDescription, packedData, true);
                            }
                            catch
                            {
                            }
                        }
                    }
                    else
                    {
                        _searchClusterTask.Aborted = true;
                    }

                    if (searchStartSessionComplete != null)
                    {
                        searchStartSessionComplete(_searchClusterTask.Aborted ? null : searchResultSet);
                    }

                    _searchClusterTask = null;
                }
            );
        }

        public override void SearchSessionEnd()
        {
            if (_searchClusterTask != null)
            {
                _searchClusterTask.Aborted = true;
            }
        }

        public override void SearchGetProgress(SearchProgressDelegate searchGetProgressComplete)
        {
            if (_searchClusterTask != null)
            {
                if (searchGetProgressComplete != null)
                {
                    searchGetProgressComplete(_searchClusterTask.GetProgress(), _searchClusterTask.IsSearching);
                }
            }
        }
        #endregion

        #region Annotation
        public override void AnnotationRenderWebPage(Guid renderTaskId, string renderUrl, AnnotationRenderWebPageComplete annotationRenderWebPageComplete)
        {
            ThreadPool.QueueUserWorkItem(delegate(object s)
            {
                string imageMimeType = "";
                int imageWidth = 0;
                int imageHeight = 0;
                _renderWebPage = null;

                try
                {
                    if (renderUrl.ToLower().EndsWith(".jpg") || renderUrl.ToLower().EndsWith(".jpeg"))
                    {
                        Obany.Communications.CommunicationsResult communicationResult = Obany.Communications.CommunicationsManager.Get(new Uri(renderUrl), null);

                        if (communicationResult.Status == OperationStatus.Success)
                        {
                            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(communicationResult.BinaryResponse))
                            {
                                using (System.Drawing.Image image = System.Drawing.Bitmap.FromStream(ms))
                                {
                                    imageMimeType = "image/jpeg";
                                    imageWidth = image.Width;
                                    imageHeight = image.Height;
                                    _renderWebPage = communicationResult.BinaryResponse;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }

                if (_renderWebPage == null)
                {
                    Obany.Render.Web.HtmlToImage h2i = new Obany.Render.Web.HtmlToImage();
                    _renderWebPage = h2i.RenderToBitmap(renderUrl, out imageWidth, out imageHeight, out imageMimeType);
                }

                if (annotationRenderWebPageComplete != null)
                {
                    annotationRenderWebPageComplete(_renderWebPage != null, renderTaskId, _renderWebPage, imageWidth, imageHeight, imageMimeType, null);
                }
            });
        }

        public override void AnnotationSave(Guid saveTaskId, System.IO.Stream saveStream, Obany.Render.Objects.Canvas canvas, string mimeType, AnnotationSaveComplete annotationSaveComplete)
        {
            ThreadPool.QueueUserWorkItem(delegate(object s)
            {
                Obany.Render.Canvas.CanvasRenderer canvasRenderer = new Obany.Render.Canvas.CanvasRenderer();

                string errorMessage;
                Obany.Render.Objects.RenderCanvas renderCanvas = canvasRenderer.Render(canvas, mimeType, GetFontFolderPath(), out errorMessage);

                if (renderCanvas != null)
                {
                    saveStream.Write(renderCanvas.Data, 0, renderCanvas.Data.Length);
                    saveStream.Close();
                }

                if (annotationSaveComplete != null)
                {
                    annotationSaveComplete(renderCanvas != null, saveTaskId);
                }
            });
        }

        public override void AnnotationCleanup()
        {
            _renderWebPage = null;
        }

        public override void AnnotationShare(Guid shareTaskId, string provider, Obany.Render.Objects.Canvas canvas, string title, bool isPublic, string keywords, AnnotationShareComplete annotationShareComplete)
        {
            ThreadPool.QueueUserWorkItem(delegate(object s)
            {
                bool success = false;
                Uri providerUri = null;

                Obany.DocumentStorage.Model.IDocumentStorageProvider documentStorageProvider = Core.CoreConfiguration.DocumentStorageProviderFactory.CreateInstance(provider);

                if (documentStorageProvider != null)
                {
                    ApiCredential apic = ApiCredentialsGet(provider);

                    if(apic != null)
                    {
                        bool sessionOk = true;

                        ISessionProvider sessionProvider = documentStorageProvider as ISessionProvider;
                        string sessionId = null;

                        if(sessionProvider != null)
                        {
                            Obany.Provider.Model.Results.SessionStartResult sessionStartResult = sessionProvider.SessionStart(
                                Obany.Cryptography.SecureEncryption.DecryptData(apic.ApiCredentials1),
                                Obany.Cryptography.SecureEncryption.DecryptData(apic.ApiCredentials2),
                                Obany.Cryptography.SecureEncryption.DecryptData(apic.ApiCredentials3),
                                Obany.Cryptography.SecureEncryption.DecryptData(apic.ApiCredentials4));

                            if(sessionStartResult.Status != OperationStatus.Success)
                            {
                                sessionOk = false;
                            }
                            else
                            {
                                sessionId = sessionStartResult.SessionId;
                            }
                        }

                        if(sessionOk)
                        {
                            Obany.Render.Canvas.CanvasRenderer canvasRenderer = new Obany.Render.Canvas.CanvasRenderer();

                            string errorMessage;
                            Obany.Render.Objects.RenderCanvas renderCanvas = canvasRenderer.Render(canvas, "application/pdf", GetFontFolderPath(), out errorMessage);

                            if (renderCanvas != null)
                            {
                                List<Obany.DocumentStorage.Model.DocumentMetadata> documentMetaData = new List<Obany.DocumentStorage.Model.DocumentMetadata>();
                                if (!string.IsNullOrEmpty(keywords))
                                {
                                    documentMetaData.Add(new Obany.DocumentStorage.Model.DocumentMetadata("Keywords", keywords));
                                }

                                Obany.DocumentStorage.Model.Results.DocumentUploadResult documentUploadResult = documentStorageProvider.DocumentUpload(title, "pdf", renderCanvas.Data, "application/pdf", isPublic, documentMetaData);

                                if (documentUploadResult.Status == OperationStatus.Success)
                                {
                                    providerUri = documentStorageProvider.GetDocumentUrl(documentUploadResult.DocumentId);
                                    success = true;
                                }
                            }

                            if (sessionProvider != null)
                            {
                                sessionProvider.SessionEnd(sessionId);
                            }

                        }
                    }
                }

                if(annotationShareComplete != null)
                {
                    annotationShareComplete(success, shareTaskId, providerUri);
                }
            });
        }
        #endregion

        #region User Storage
        private void UserStorageInitialiseLocal()
        {
            if (!System.IO.Directory.Exists(_configurationFolder))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_configurationFolder);
                }
                catch
                {
                }
            }

            if (System.IO.Directory.Exists(_configurationFolder))
            {
                HistoryStorageInitialiseLocal();

                _thumbnailCacheFolder = System.IO.Path.Combine(_configurationFolder, "ThumbnailCache");

                if (!System.IO.Directory.Exists(_thumbnailCacheFolder))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(_thumbnailCacheFolder);

                    }
                    catch
                    {
                    }
                }
            }
            _thumbnailCacheSpaceUsed = ThumbnailCacheSpaceUsed();
        }

        private string UserStorageGetRoot()
        {
            return (_configurationFolder + "\\");
        }

        private void UserStorageDelete(string filename)
        {
            string fullFilename = System.IO.Path.Combine(_configurationFolder, filename);

            try
            {
                if (System.IO.File.Exists(fullFilename))
                {
                    System.IO.File.Delete(fullFilename);
                }
            }
            catch
            {
            }
        }

        private byte[] UserStorageLoad(string filename)
        {
            byte[] data = null;

            try
            {
                string fullFilename = System.IO.Path.Combine(_configurationFolder, filename);

                if (System.IO.File.Exists(fullFilename))
                {
                    data = System.IO.File.ReadAllBytes(fullFilename);
                }
            }
            catch
            {
            }

            return (data);
        }

        private void UserStorageSave(string filename, byte[] data)
        {
            try
            {
                string fullFilename = System.IO.Path.Combine(_configurationFolder, filename);
                System.IO.File.WriteAllBytes(fullFilename, data);
            }
            catch
            {
            }
        }
        #endregion

        #region Configuration Storage
        private void ConfigurationLoadLocal()
        {
            byte[] data = UserStorageLoad("Config.xml");
            ConfigurationDeserialise(data);
        }

        private void ConfigurationSaveLocal()
        {
            byte[] data = ConfigurationSerialise();
            UserStorageSave("Config.xml", data);
        }
        #endregion


        #region Favourites Storage
        private void FavouritesLoadLocal()
        {
            byte[] data = UserStorageLoad("Favourites.xml");
            FavouritesDeserialise(data);
            InvokePropertyChanged("Favourites");
        }

        private void FavouritesSaveLocal()
        {
            byte[] data = FavouritesSerialise();
            UserStorageSave("Favourites.xml", data);
        }
        #endregion

        #region History Storage
        private void HistoryStorageInitialiseLocal()
        {
            string fullpath = System.IO.Path.Combine(_configurationFolder, "SearchHistory");

            if (!System.IO.Directory.Exists(fullpath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(fullpath);
                }
                catch
                {
                }
            }
        }

        private void HistoryLoadLocal()
        {
            byte[] data = UserStorageLoad("History.xml");
            HistoryDeserialise(data);
            InvokePropertyChanged("History");
        }

        private void HistorySaveLocal()
        {
            byte[] data = HistorySerialise();
            UserStorageSave("History.xml", data);
        }

        public override long HistorySpaceUsed()
        {
            long totalSize = 0;

            try
            {
                foreach (string file in Directory.GetFiles(System.IO.Path.Combine(_configurationFolder, SearchResultSetGetFolder())))
                {
                    FileInfo fi = new FileInfo(file);

                    totalSize += fi.Length;
                }
            }
            catch { }

            return (totalSize);
        }
        #endregion

        #region Search Result Sets Storage
        string SearchResultSetGetFolder()
        {
            return ("SearchHistory\\");
        }

        string SearchResultSetGetFilename(Guid id)
        {
            return (SearchResultSetGetFolder() + id.ToString("N") + ".xml");
        }

        byte[] SearchResultSetLoadLocal(Guid resultSetId)
        {
            return (UserStorageLoad(SearchResultSetGetFilename(resultSetId)));
        }

        void SearchResultSetSaveLocal(SearchDescription searchDescription, byte[] data, bool addToHistory)
        {
            UserStorageInitialiseLocal();

            string filename = SearchResultSetGetFilename(searchDescription.Id);

            if (addToHistory)
            {
                UserStorageSave(filename, data);

                _searchHistory.Insert(0, searchDescription);

                HistoryCleanup();

                HistorySaveLocal();
            }
            else
            {
                UserStorageSave(filename, data);
            }
        }

        void SearchResultSetDeleteLocal(Guid resultSetId)
        {
            UserStorageDelete(SearchResultSetGetFilename(resultSetId));
        }

        void SearchResultSetsClearLocal()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine(_configurationFolder, SearchResultSetGetFolder()), "*.xml");

                foreach(string file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Thumbnail Cache
        public override void ThumbnailCacheCleanup(long additionalSize)
        {
            try
            {
                long maxLimit = _thumbnailCacheSize * 1024 * 1024;

                if (_thumbnailCacheSpaceUsed + additionalSize > maxLimit)
                {
                    List<FileInfo> allFiles = new List<FileInfo>();

                    long totalSize = 0;

                    foreach (string file in Directory.GetFiles(_thumbnailCacheFolder))
                    {
                        FileInfo fi = new FileInfo(file);

                        allFiles.Add(fi);

                        totalSize += fi.Length;
                    }

                    allFiles.Sort(
                        delegate(FileInfo x, FileInfo y)
                        {
                            return (x.LastWriteTime.CompareTo(y.LastWriteTime));
                        }
                    );

                    while (totalSize > maxLimit)
                    {
                        totalSize -= allFiles[0].Length;
                        try
                        {
                            System.IO.File.Delete(allFiles[0].FullName);
                        }
                        catch { }
                        allFiles.RemoveAt(0);
                    }

                    _thumbnailCacheSpaceUsed = totalSize;
                }
                else
                {
                    _thumbnailCacheSpaceUsed += additionalSize;
                }
            }
            catch { }
        }

        public override void ThumbnailCacheClear()
        {
            try
            {
                foreach (string file in Directory.GetFiles(_thumbnailCacheFolder))
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch { }               
                }


                _thumbnailCacheSpaceUsed = ThumbnailCacheSpaceUsed();
            }
            catch { }
        }

        public override long ThumbnailCacheSpaceUsed()
        {
            long totalSize = 0;

            try
            {
                foreach (string file in Directory.GetFiles(_thumbnailCacheFolder))
                {
                    FileInfo fi = new FileInfo(file);

                    totalSize += fi.Length;
                }
            }
            catch { }

            return (totalSize);
        }
        #endregion

        #region Interop
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [System.Runtime.InteropServices.Out] StringBuilder pszPath);

        public static string GetFontFolderPath()
        {
            StringBuilder sb = new StringBuilder();
            SHGetFolderPath(IntPtr.Zero, 0x0014, IntPtr.Zero, 0x0000, sb);

            return sb.ToString();
        }
        #endregion
    }
}
