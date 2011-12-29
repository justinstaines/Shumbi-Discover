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
using Obany.Search.Model;
using System.Threading;
using Obany.Cluster.Model;

namespace ShumbiDiscover.Core
{
    /// <summary>
    /// Class to manage the searching task
    /// </summary>
    class SearchClusterTask
    {
        #region Fields
        private string _query;
        private int _numResults;
        private string _culture;
        private Dictionary<string, ISearchProvider> _searchProviders;
        private Dictionary<string, SearchAggregateItem> _resultsDictionary;
        private int _runningCount;
        private bool _aborted;
        private bool _isSearching;
        #endregion

        #region Constuctors
        public SearchClusterTask()
        {
        }
        #endregion

        #region Properties
        public bool Aborted
        {
            get
            {
                return (_aborted);
            }
            set
            {
                _aborted = value;
            }
        }

        public bool IsSearching
        {
            get
            {
                return (_isSearching);
            }
        }
        #endregion

        #region Public Methods
        public List<SearchAggregateItem> Search(List<string> searchProviders, string query, int numResults, string culture)
        {
            List<SearchAggregateItem> results = new List<SearchAggregateItem>();

            _searchProviders = new Dictionary<string, ISearchProvider>();
            _resultsDictionary = new Dictionary<string, SearchAggregateItem>();

            _isSearching = true;

            _query = query;
            _numResults = numResults;
            _runningCount = 0;
            _culture = culture;
            _aborted = false;

            lock (_searchProviders)
            {
                foreach (string searchProvider in searchProviders)
                {
                    ISearchProvider isp = ShumbiDiscover.Core.CoreConfiguration.SearchProviderFactory.CreateInstance(searchProvider);

                    if (isp != null)
                    {
                        _searchProviders.Add(searchProvider, isp);
                    }
                }
            }

            _runningCount = _searchProviders.Count;

            lock (_searchProviders)
            {
                foreach (KeyValuePair<string, ISearchProvider> kvp in _searchProviders)
                {
                    ThreadPool.QueueUserWorkItem(DoSearch, kvp.Value);
                }
            }

            while (_runningCount > 0 && !_aborted)
            {
                Thread.Sleep(50);
            }

            if (!_aborted)
            {
                foreach (KeyValuePair<string, SearchAggregateItem> kvp in _resultsDictionary)
                {
                    results.Add(kvp.Value);
                }
            }

            return (results);
        }

        public List<SearchCluster> Cluster(string query, string culture)
        {
            _isSearching = false;

            List<ISnippet> snippets = new List<ISnippet>();

            foreach (KeyValuePair<string, SearchAggregateItem> kvp in _resultsDictionary)
            {
                ClustererSnippet clusterDocument = new ClustererSnippet();

                clusterDocument.Id = kvp.Value.Id;
                clusterDocument.Title = kvp.Value.Title;
                clusterDocument.Body = kvp.Value.Description;
                clusterDocument.Location = kvp.Value.OpenUrl;

                snippets.Add(clusterDocument);
            }

            IList<ICluster> resultClusters = Core.CoreConfiguration.ClusterProvider.Cluster(query, snippets, culture);

            List<SearchCluster> clusters = new List<SearchCluster>();

            ConvertClusters(resultClusters, clusters);

            return (clusters);
        }

        public Dictionary<string, int> GetProgress()
        {
            Dictionary<string, int> progress = new Dictionary<string, int>();

            if (_searchProviders != null)
            {
                lock (_searchProviders)
                {
                    foreach (KeyValuePair<string, ISearchProvider> kvp in _searchProviders)
                    {
                        progress.Add(kvp.Key, kvp.Value.ResultCount);
                    }
                }
            }

            return (progress);
        }

        #endregion

        #region Private Methods
        void DoSearch(object state)
        {
            IList<ISearchResult> results = (state as ISearchProvider).Search(_query, _numResults, _culture);

            if (results != null)
            {
                lock (_resultsDictionary)
                {
                    foreach (ISearchResult searchResult in results)
                    {
                        string resultKey = searchResult.OpenUrl.ToLower();

                        SearchProviderRank searchProviderRank = new SearchProviderRank();
                        searchProviderRank.Provider = searchResult.Provider;
                        searchProviderRank.Rank = searchResult.Rank;

                        if (_resultsDictionary.ContainsKey(resultKey))
                        {
                            _resultsDictionary[resultKey].ProviderRanks.Add(searchProviderRank);
                        }
                        else
                        {
                            SearchAggregateItem searchAggregateItem = new SearchAggregateItem();
                            searchAggregateItem.Id = Guid.NewGuid();
                            searchAggregateItem.Title = Obany.Communications.HtmlHelper.Cleanup(searchResult.Title);
                            searchAggregateItem.Description = Obany.Communications.HtmlHelper.Cleanup(searchResult.Description);
                            searchAggregateItem.OpenUrl = searchResult.OpenUrl;
                            searchAggregateItem.ThumbnailUrl = searchResult.ThumbnailUrl;
                            searchAggregateItem.ContentUrl = searchResult.ContentUrl;
                            searchAggregateItem.Kind = searchResult.Kind.ToString();
                            searchAggregateItem.ProviderRanks = new List<SearchProviderRank>();
                            searchAggregateItem.ProviderRanks.Add(searchProviderRank);

                            _resultsDictionary.Add(resultKey, searchAggregateItem);
                        }
                    }
                }
            }

            _runningCount--;
        }

        private void ConvertClusters(IList<ICluster> resultClusters, List<SearchCluster> clusters)
        {
            foreach (ICluster resultCluster in resultClusters)
            {
                SearchCluster searchCluster = new SearchCluster();
                searchCluster.Title = resultCluster.Title;
                searchCluster.Score = resultCluster.Score;
                searchCluster.SearchResultIds = new List<System.Guid>();
                foreach (ISnippet snippet in resultCluster.Snippets)
                {
                    searchCluster.SearchResultIds.Add((snippet as ClustererSnippet).Id);
                }
                searchCluster.SearchClusters = new List<SearchCluster>();
                clusters.Add(searchCluster);

                ConvertClusters(resultCluster.SubClusters, searchCluster.SearchClusters);
            }
        }
        #endregion


    }
}
