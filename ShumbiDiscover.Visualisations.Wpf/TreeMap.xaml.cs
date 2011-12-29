#region Copyright Statement
// Copyright © 2009 Shumbi Ltd
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Obany.Core;
using Obany.UI;
using ShumbiDiscover.Model;
using Obany.UI.Controls;

namespace ShumbiDiscover.Visualisations
{
    /// <summary>
    /// Class to display a tree map visualisation
    /// </summary>
    public partial class TreeMap : UserControl, IVisualisation, ILocalisable
    {
        #region Fields
        private object _originalRoot;
        private object _currentRoot;
        private object _selectedNode;
        private Rectangle _selectedRect;
        private Dictionary<object, object> _parents;
        private long _lastMouseUp;
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
        public TreeMap()
        {
            InitializeComponent();

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
                return ("TreeMap");
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
            _selectedRect = null;
            _selectedNode = null;

            treeMap.ItemsSource = root as IEnumerable;

            buttonUpOneLevel.Visibility = _currentRoot == _originalRoot ? Visibility.Collapsed : Visibility.Visible;
        }
        #endregion

        #region Control Event Handlers
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            long now = DateTime.Now.Ticks;

            Grid g = sender as Grid;

            if (g != null)
            {
                object lastnode = _selectedNode;
                _selectedNode = g.Tag;

                if (_selectedRect != null)
                {
                    _selectedRect.Opacity = 0;
                }

                _selectedRect = ((g.Children[0] as Border).Child as Grid).Children[0] as Rectangle;
                _selectedRect.Opacity = 0.5;

                if (ItemSelected != null)
                {
                    ItemSelected(_selectedNode);
                }

                if (now - _lastMouseUp < TimeSpan.FromMilliseconds(300).Ticks && _selectedNode == lastnode)
                {
                    int childCount = 0;
                    ICollection collection = _selectedNode as ICollection;
                    if (collection != null)
                    {
                        childCount = collection.Count;
                    }

                    if (childCount > 0)
                    {
                        if (!_parents.ContainsKey(_selectedNode))
                        {
                            _parents.Add(_selectedNode, _currentRoot);
                        }
                        PopulateItems(_selectedNode);
                    }
                    else
                    {
                        if (ItemActivated != null)
                        {
                            ItemActivated(_selectedNode);
                        }
                    }
                }

                _lastMouseUp = now;
            }

            e.Handled = true;
        }

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
