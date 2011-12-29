#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Obany.Core;
using Obany.UI;
using ShumbiDiscover.Model;

namespace ShumbiDiscover.Visualisations
{
    /// <summary>
    /// Tag Cloud Control
    /// </summary>
    public partial class TagCloud : UserControl, IVisualisation, ILocalisable
    {
        #region Fields
        private WrapPanel _panelItems;
        private TagCloudVisualNode _selectedNode;
        private object _originalRoot;
        private object _currentRoot;
        private Dictionary<object, object> _parents;
        #endregion

        #region Events
        /// <summary>
        /// Event to fire when an item is activated
        /// </summary>
        public event ItemActivatedEventHandler ItemActivated;
        /// <summary>
        /// Event to fire when an item is selected
        /// </summary>
        public event ItemSelectedEventHandler ItemSelected;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public TagCloud()
        {
            InitializeComponent();

            _panelItems = new WrapPanel();
            _panelItems.Margin = new Thickness(30, 30, 30, 30);

            scrollViewer.Content = _panelItems;

            _parents = new Dictionary<object, object>();
        }
        #endregion

        #region IVisualisation Properties
        /// <summary>
        /// Get the name
        /// </summary>
        public string VisualisationName
        {
            get
            {
                return ("TagCloud");
            }
        }

        /// <summary>
        /// Set the items root
        /// </summary>
        public object ItemsRoot
        {
            set
            {
                _originalRoot = value;
                PopulateItems(value);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Localise the control
        /// </summary>
        public void Localise()
        {
            labelUpOneLevel.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "UPONELEVEL");
        }

        /// <summary>
        /// Hook the global hid events
        /// </summary>
        /// <param name="hook"></param>
        public void HookHidEvents(bool hook)
        {
        }
        #endregion

        #region Private Methods
        private void PopulateItems(object root)
        {
            _currentRoot = root;

            _panelItems.Children.Clear();

            double minValue = 1;
            double maxValue = 0;
            double minPixel = 20;
            double maxPixel = 40;

            IEnumerable enumerable = _currentRoot as IEnumerable;

            if (enumerable != null)
            {
                IEnumerator iEnum = enumerable.GetEnumerator();

                iEnum.Reset();
                while(iEnum.MoveNext())
                {
                    IRelevance relevance = iEnum.Current as IRelevance;

                    if (relevance != null)
                    {
                        if (relevance.Relevance != -1)
                        {
                            if (relevance.Relevance < minValue)
                            {
                                minValue = relevance.Relevance;
                            }
                            if (relevance.Relevance > maxValue)
                            {
                                maxValue = relevance.Relevance;
                            }
                        }
                    }
                }

                double weightRange = maxValue - minValue;
                if (weightRange == 0)
                {
                    weightRange = 1;
                }

                double pixelWeightScale = (maxPixel - minPixel) / weightRange;

                iEnum.Reset();
                while (iEnum.MoveNext())
                {
                    TagCloudVisualNode tagCloudVisualNode = new TagCloudVisualNode();
                    tagCloudVisualNode.NodeActivated += new TagCloudVisualNode.NodeActivatedEventHandler(tagCloudVisualNode_NodeActivated);
                    tagCloudVisualNode.NodeSelected += new TagCloudVisualNode.NodeSelectedEventHandler(tagCloudVisualNode_NodeSelected);
                    tagCloudVisualNode.ContentObject = iEnum.Current;

                    double relval = 1;
                    IRelevance relevance = iEnum.Current as IRelevance;

                    if (relevance != null)
                    {
                        relval = relevance.Relevance;
                    }

                    tagCloudVisualNode.LabelFontSize = minPixel + (((relval == -1 ? minValue : relval) - minValue) * pixelWeightScale);
                    _panelItems.Children.Add(tagCloudVisualNode);
                }
            }

            buttonUpOneLevel.Visibility = _currentRoot == _originalRoot ? Visibility.Collapsed : Visibility.Visible;
        }

        void tagCloudVisualNode_NodeSelected(TagCloudVisualNode node)
        {
            if (_selectedNode != null)
            {
                _selectedNode.IsSelected = false;
            }

            _selectedNode = node;

            if (_selectedNode != null)
            {
                _selectedNode.IsSelected = true;
            }

            if (ItemSelected != null)
            {
                ItemSelected(node == null ? null : node.ContentObject);
            }
        }

        void tagCloudVisualNode_NodeActivated(TagCloudVisualNode node)
        {
            int childCount = 0;
            ICollection collection = node.ContentObject as ICollection;
            if (collection != null)
            {
                childCount = collection.Count;
            }

            if (childCount > 0)
            {
                _selectedNode = null;
                if (!_parents.ContainsKey(node.ContentObject))
                {
                    _parents.Add(node.ContentObject, _currentRoot);
                }
                PopulateItems(node.ContentObject);
            }
            else
            {
                if (ItemActivated != null)
                {
                    ItemActivated(node.ContentObject);
                }
            }
        }
        #endregion

        #region Control Event Handlers
        private void buttonUpOneLevel_Click(object sender, RoutedEventArgs e)
        {
            if (_parents.ContainsKey(_currentRoot))
            {
                PopulateItems(_parents[_currentRoot]);
            }
        }
        #endregion
    }
}
