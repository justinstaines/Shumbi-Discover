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
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Obany.Core;
using Obany.UI;
using ShumbiDiscover.Model;

namespace ShumbiDiscover.Visualisations
{
    /// <summary>
    /// Class to display a network of visual items
    /// </summary>
    public partial class NetworkControl : UserControl, IVisualisation, ILocalisable
    {
        #region Fields
        private object _rootNode;
        private List<object> _visibleNodes;
        private List<KeyValuePair<object, object>> _edges;
        private Dictionary<object, NetworkVisualNode> _visualItems;
        private Dictionary<Line, KeyValuePair<NetworkVisualNode, NetworkVisualNode>> _connectors;
        private Dictionary<object, object> _nodeParents;
        private Dictionary<object, Point> _nodeCentre;
        private Dictionary<object, Vector> _nodeVelocity;
        private Dictionary<object, Vector> _nodeAcceleration;

        private DispatcherTimer _updateTimer;

        private double _initRadius;

        private long _settlingStart;

        private Point _scrollCentre;

        // Used when manually scrolling.
        private Point _scrollStartPoint;

        private bool _isMouseCaptured;

        private NetworkVisualNode _selectedNode;

        private Size _canvasDimensions;
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
        /// Default constructor
        /// </summary>
        public NetworkControl()
        {
            InitializeComponent();

            _canvasDimensions = new Size(25000, 25000);
            _scrollCentre = new Point(0.5, 0.5);

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateTimer.Tick += new EventHandler(_updateTimer_Tick);

            ClearItems();

            ItemActivated = null;
            ItemSelected = null;

            gridZoom.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(NetworkControl_MouseLeftButtonDown);
            gridZoom.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(NetworkControl_MouseLeftButtonUp);
            gridZoom.MouseMove += new System.Windows.Input.MouseEventHandler(NetworkControl_MouseMove);

            gridZoom.MouseWheel += new MouseWheelEventHandler(NetworkControl_MouseWheel);

            _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            ResetSliders();

            sliderZoom.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sliderZoom_ValueChanged);
            sliderRelevance.ValueChanged += new RoutedPropertyChangedEventHandler<double>(sliderRelevance_ValueChanged);

#if SILVERLIGHT
            _scrollViewer.LayoutUpdated += new EventHandler(_scrollViewer_LayoutUpdated);
#else
            _scrollViewer.ScrollChanged += new ScrollChangedEventHandler(_scrollViewer_ScrollChanged);
#endif
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
                return ("Network");
            }
        }

        /// <summary>
        /// Set the items root
        /// </summary>
        public object ItemsRoot
        {
            set
            {
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
            labelZoom.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "ZOOM");
            labelRelevance.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "RELEVANCE");
        }

        /// <summary>
        /// Hook the global hid events
        /// </summary>
        /// <param name="hook"></param>
        public void HookHidEvents(bool hook)
        {
#if !SILVERLIGHT
            if (hook)
            {
                Obany.Hid.Tablet.TabletManager.HidScroll += new Obany.Hid.Tablet.TabletManager.HidScrollEventHandler(TabletManager_HidScroll);
            }
            else
            {
                Obany.Hid.Tablet.TabletManager.HidScroll -= new Obany.Hid.Tablet.TabletManager.HidScrollEventHandler(TabletManager_HidScroll);
            }
#endif
        }
        #endregion

        /// <summary>
        /// Populate the network with the tree
        /// </summary>
        /// <param name="root">The root of the tree</param>
        private void PopulateItems(object root)
        {
            ClearItems();

            _rootNode = root;

            _initRadius = Math.Min(this.ActualWidth / 2, this.ActualHeight / 2);

            _canvasDimensions = new Size(50000, 50000);
            _scrollCentre = new Point(0.5, 0.5);

            _visibleNodes.Add(root);
            _nodeCentre.Add(root, new Point(_canvasDimensions.Width / 2, _canvasDimensions.Height / 2));
            _visualItems.Add(root, CreateVisualItem(root));
            _nodeAcceleration.Add(root, new Vector());
            _nodeVelocity.Add(root, new Vector());

            _visualItems[root].IsExpanded = true;

            _nodeParents.Add(root, null);

            SetLocations(root, new Point(_canvasDimensions.Width / 2, _canvasDimensions.Height / 2));

            _settlingStart = DateTime.Now.Ticks;
            _updateTimer.Start();

            SetZoom(sliderZoom.Value);
        }

        /// <summary>
        /// Clear the items in the network
        /// </summary>
        public void ClearItems()
        {
            _visibleNodes = new List<object>();
            _edges = new List<KeyValuePair<object, object>>();
            _connectors = new Dictionary<Line, KeyValuePair<NetworkVisualNode, NetworkVisualNode>>();
            _visualItems = new Dictionary<object, NetworkVisualNode>();
            _nodeParents = new Dictionary<object, object>();
            _nodeCentre = new Dictionary<object, Point>();
            _nodeVelocity = new Dictionary<object, Vector>();
            _nodeAcceleration = new Dictionary<object, Vector>();
            _canvas.Children.Clear();
            _selectedNode = null;

            ResetSliders();
        }

        private void SetLocations(object parent, Point parentCentre)
        {
            IEnumerable enumerable = parent as IEnumerable;

            if(enumerable != null)
            {
                IEnumerator iEnum = enumerable.GetEnumerator();

                int itemCount = 0;

                ICollection collection = parent as ICollection;
                if (collection != null)
                {
                    itemCount = collection.Count;
                }
                else
                {
                    iEnum.Reset();
                    while (iEnum.MoveNext())
                    {
                        itemCount++;
                    }
                }

                double maxRadius = Math.Min(this.ActualWidth / 2 - 50, this.ActualHeight / 2 - 50);
                double splitCounter = 1.0 / itemCount;
                double itemsPerRotation = 15;

                // First add all the items in this bubble
                int i = 0;
                iEnum.Reset();
                while(iEnum.MoveNext())
                {
                    if (!_visibleNodes.Contains(iEnum.Current))
                    {
                        double numRotations = Math.Ceiling(itemCount / itemsPerRotation);
                        double d = ((splitCounter * i) * numRotations) * Math.PI;

                        _visibleNodes.Add(iEnum.Current);
                        _nodeVelocity.Add(iEnum.Current, new Vector(100 * Math.Cos(d), 100 * Math.Sin(d)));
                        _nodeAcceleration.Add(iEnum.Current, new Vector());
                        _visualItems.Add(iEnum.Current, CreateVisualItem(iEnum.Current));

                        KeyValuePair<object, object> edge = new KeyValuePair<object, object>(parent, iEnum.Current);
                        _edges.Add(edge);

                        KeyValuePair<NetworkVisualNode, NetworkVisualNode> connector = new KeyValuePair<NetworkVisualNode, NetworkVisualNode>(_visualItems[parent], _visualItems[iEnum.Current]);
                        _connectors.Add(CreateVisualLine(), connector);

                        _nodeParents.Add(iEnum.Current, parent);

                        _nodeCentre.Add(iEnum.Current, new Point(parentCentre.X + ((splitCounter * i) * maxRadius) - maxRadius / 2, parentCentre.Y + ((splitCounter * i) * maxRadius) - maxRadius / 2));
                    }
                    i++;
                }

                iEnum.Reset();
                while(iEnum.MoveNext())
                {
                    if (_visualItems[iEnum.Current].IsExpanded)
                    {
                        SetLocations(iEnum.Current, _nodeCentre[iEnum.Current]);
                    }
                }
            }
        }

        private void ResetSliders()
        {
            sliderRelevance.Minimum = 0.99f;
            sliderRelevance.Maximum = 1f;
            sliderRelevance.Value = 0.99f;
            sliderRelevance.SmallChange = (sliderRelevance.Maximum - sliderRelevance.Minimum) / 100;
            sliderRelevance.LargeChange = (sliderRelevance.Maximum - sliderRelevance.Minimum) / 10;

            sliderZoom.Minimum = 0.15;
            sliderZoom.Maximum = 3;
            sliderZoom.SmallChange = (sliderZoom.Maximum - sliderZoom.Minimum) / 100;
            sliderZoom.LargeChange = (sliderZoom.Maximum - sliderZoom.Minimum) / 10;
            sliderZoom.Value = 1;
        }

        private void UpdateNodeLocations()
        {
            foreach (object node in _visibleNodes)
            {
                _visualItems[node].Centre = _nodeCentre[node];
            }
        }

        private NetworkVisualNode CreateVisualItem(object tag)
        {
            IRelevance relevance = tag as IRelevance;

            if (relevance != null)
            {
                if (relevance.Relevance < sliderRelevance.Minimum && relevance.Relevance != -1)
                {
                    sliderRelevance.Minimum = relevance.Relevance;
                    sliderRelevance.Value = relevance.Relevance;
                }
            }

            NetworkVisualNode visualNode = new NetworkVisualNode();
            visualNode.ContentObject = tag;
            visualNode.NodeSelected += new NetworkVisualNode.NodeSelectedEventHandler(visualNode_NodeSelected);
            visualNode.NodeActivated += new NetworkVisualNode.NodeActivatedEventHandler(visualNode_NodeActivated);
            visualNode.NodeHovered += new NetworkVisualNode.NodeHoveredEventHandler(visualNode_NodeHovered);
            visualNode.UpdateRelevence(sliderRelevance.Value <= sliderRelevance.Minimum ? -1 : sliderRelevance.Value);

            _canvas.Children.Add(visualNode);

            return (visualNode);
        }

        void visualNode_NodeHovered(NetworkVisualNode node)
        {
            // When hovered move it to the front of all the other objects
            _canvas.Children.Remove(node);
            _canvas.Children.Add(node);
        }

        void visualNode_NodeSelected(NetworkVisualNode node)
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

        void visualNode_NodeActivated(NetworkVisualNode node)
        {
            int childCount = 0;

            ICollection collection = node.ContentObject as ICollection;

            if(collection != null)
            {
                childCount = collection.Count;
            }

            if (childCount > 0)
            {
                // Expand the items, or collapse if it is already expanded
                node.IsExpanded = !node.IsExpanded;

                if (!node.IsExpanded)
                {
                    RemoveChildNodes(node.ContentObject);
                }

                object parent = _nodeParents[node.ContentObject];
                if (parent == null)
                {
                    parent = _rootNode;
                }

                SetLocations(parent, _nodeCentre[parent]);

                _settlingStart = DateTime.Now.Ticks;
                _updateTimer.Start();

                ScrollToPoint(_nodeCentre[node.ContentObject]);
            }
            else
            {
                // Has no children so activated it instead
                if (ItemActivated != null)
                {
                    ItemActivated(node.ContentObject);
                }
            }
        }

        private void SetZoom(double zoomValue)
        {
            ScaleTransform st = new ScaleTransform();
            st.ScaleX = zoomValue;
            st.ScaleY = zoomValue;
            _canvas.RenderTransform = st;

            // Adjust the size of the zoom grid so that the scrollviewer redraws the scroll bars
            gridZoom.Width = _canvasDimensions.Width * zoomValue;
            gridZoom.Height = _canvasDimensions.Height * zoomValue;

            UpdateScrollOffsets();
        }

        private void UpdateScrollOffsets()
        {
            double horizontalOffset;
            double verticalOffset;

            if (_scrollViewer.ViewportWidth > gridZoom.Width)
            {
                horizontalOffset = 0;
            }
            else
            {
                horizontalOffset = (_scrollCentre.X * gridZoom.Width) - (_scrollViewer.ViewportWidth / 2);
            }
            if (_scrollViewer.ViewportHeight > gridZoom.Height)
            {
                verticalOffset = 0;
            }
            else
            {
                verticalOffset = (_scrollCentre.Y * gridZoom.Height) - (_scrollViewer.ViewportHeight / 2);
            }
            _scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            _scrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        private void ScrollToPoint(Point point)
        {
            _scrollCentre = new Point(point.X / _canvasDimensions.Width, point.Y / _canvasDimensions.Height);

            UpdateScrollOffsets();
        }

        private void RemoveChildNodes(object root)
        {
            IEnumerable enumerable = root as IEnumerable;

            if (enumerable != null)
            {
                IEnumerator iEnum = enumerable.GetEnumerator();

                while(iEnum.MoveNext())
                {
                    object item = iEnum.Current;

                    if (_visualItems.ContainsKey(item))
                    {
                        if (_visualItems[item].IsExpanded)
                        {
                            RemoveChildNodes(item);
                        }

                        _canvas.Children.Remove(_visualItems[item]);
                        _visualItems.Remove(item);
                    }

                    _visibleNodes.Remove(item);
                    _nodeParents.Remove(item);
                    _nodeCentre.Remove(item);
                    _nodeVelocity.Remove(item);
                    _nodeAcceleration.Remove(item);

                    List<KeyValuePair<object, object>> removeEdges = new List<KeyValuePair<object, object>>();
                    List<Line> removeConnectors = new List<Line>();

                    foreach (KeyValuePair<object, object> edge in _edges)
                    {
                        if (edge.Key == item || edge.Value == item)
                        {
                            removeEdges.Add(edge);
                        }
                    }

                    foreach (KeyValuePair<Line, KeyValuePair<NetworkVisualNode, NetworkVisualNode>> connector in _connectors)
                    {
                        if (connector.Value.Key.ContentObject == item ||
                            connector.Value.Value.ContentObject == item)
                        {
                            removeConnectors.Add(connector.Key);
                        }
                    }

                    foreach (KeyValuePair<object, object> removeEdge in removeEdges)
                    {
                        _edges.Remove(removeEdge);
                    }

                    foreach (Line line in removeConnectors)
                    {
                        _canvas.Children.Remove(line);
                        _connectors.Remove(line);
                    }
                }
            }
        }

        private Line CreateVisualLine()
        {
            Line line = new Line();

            line.StrokeDashArray.Add(2.0);
            line.StrokeDashArray.Add(2.0);
            line.Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0xAA, 0xAA, 0xAA));
            line.StrokeThickness = 2;

            _canvas.Children.Insert(0, line);

            return (line);
        }

        void _updateTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Ticks - _settlingStart < TimeSpan.FromSeconds(1).Ticks)
            {
                for (int i = 0; i < 20; i++)
                {
                    AnimatePositions();
                }
                UpdateNodeLocations();
                RenderConnectors();

                if (_selectedNode != null)
                {
                    ScrollToPoint(_selectedNode.Centre);
                }
            }
            else
            {
                _updateTimer.Stop();
            }
        }

        void RenderConnectors()
        {
            foreach (KeyValuePair<Line, KeyValuePair<NetworkVisualNode, NetworkVisualNode>> kvp in _connectors)
            {
                NetworkVisualNode nvn1 = kvp.Value.Key;
                NetworkVisualNode nvn2 = kvp.Value.Value;

                kvp.Key.X1 = nvn1.Centre.X;
                kvp.Key.Y1 = nvn1.Centre.Y;
                kvp.Key.X2 = nvn2.Centre.X;
                kvp.Key.Y2 = nvn2.Centre.Y;
            }
        }

        #region Physics
        internal void AnimatePositions()
        {
            // Attract using connections
            foreach (KeyValuePair<object, object> kvp in _edges)
            {
                object node1 = kvp.Key;
                object node2 = kvp.Value;

                Attract(node1, node2);
                Attract(node2, node1);
            }

            // Repulse using nodes
            foreach (object node1 in _visibleNodes)
            {
                foreach (object node2 in _visibleNodes)
                {
                    Repulse(node1, node2);
                }
            }

            // Update Positions

            foreach (object node1 in _visibleNodes)
            {
                if (node1 != _rootNode)
                {
                    Move(node1);
                }
            }
        }

        internal void Attract(object node1, object node2)
        {
            if (node1 != node2)
            {
                if (_nodeCentre.ContainsKey(node1) && _nodeCentre.ContainsKey(node2))
                {
                    Point node1Location = _nodeCentre[node1];
                    Point node2Location = _nodeCentre[node2];

                    Vector vector = new Vector(node2Location.X, node2Location.Y) - new Vector(node1Location.X, node1Location.Y);

                    if (_nodeAcceleration.ContainsKey(node2))
                    {
                        double num = 20 - vector.Length;
                        _nodeAcceleration[node2] += (Vector)((num * vector) / 750);
                    }
                }
            }
        }

        internal void Repulse(object node1, object node2)
        {
            if (node1 != node2)
            {
                if (_nodeCentre.ContainsKey(node1) && _nodeCentre.ContainsKey(node2))
                {
                    Point node1Location = _nodeCentre[node1];
                    Point node2Location = _nodeCentre[node2];

                    if (_nodeParents.ContainsKey(node1) && _nodeParents.ContainsKey(node2))
                    {
                        object node1Parent = _nodeParents[node1];
                        object node2Parent = _nodeParents[node2];

                        if (_nodeAcceleration.ContainsKey(node2))
                        {
                            Vector vector = new Vector(node2Location.X, node2Location.Y) - new Vector(node1Location.X, node1Location.Y);
                            if (node1Parent == node2Parent && node1Parent == _rootNode)
                            {
                                _nodeAcceleration[node2] += (Vector)(((_initRadius / 2) * vector) / ((vector.LengthSquared / 11) + 3));
                            }
                            else
                            {
                                _nodeAcceleration[node2] += (Vector)((30 * vector) / ((vector.LengthSquared / 11) + 3));
                            }
                        }
                    }
                }
            }
        }

        internal void Move(object node)
        {
            if (_nodeCentre.ContainsKey(node) &&
                _nodeAcceleration.ContainsKey(node) &&
                _nodeVelocity.ContainsKey(node) &&
                _nodeParents.ContainsKey(node))
            {
                Point nodeLocation = _nodeCentre[node];

                double dt = 0.5;
                double friction = 0.1;

                Vector vector = _nodeVelocity[node] + ((Vector)(_nodeAcceleration[node] * dt));
                Vector vector2 = (Vector)(-friction * vector);

                _nodeAcceleration[node] += vector2;
                _nodeVelocity[node] += (Vector)(_nodeAcceleration[node] * dt);

                Vector oldposition = new Vector(nodeLocation.X, nodeLocation.Y);
                Vector newposition = oldposition + (Vector)(_nodeVelocity[node] * dt);

                _nodeAcceleration[node] = new Vector();

                Point parentLocation = _nodeCentre[_nodeParents[node]];

                Point newLocation = new Point(newposition.X, newposition.Y);

                Vector vDiff = new Vector(parentLocation.X - newLocation.X, parentLocation.Y - newLocation.Y);

                if (vDiff.Length < _initRadius * 2)
                {
                    _nodeCentre[node] = newLocation;
                }
            }
        }
        #endregion

        void NetworkControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Save starting point, used later when determining how much to scroll.
            _scrollStartPoint = e.GetPosition(this);

            // Update the cursor if can scroll or not.
#if SILVERLIGHT
            gridZoom.Cursor = Cursors.Hand;
#else
            gridZoom.Cursor = Cursors.ScrollAll;
#endif
            gridZoom.CaptureMouse();

            _isMouseCaptured = true;

            e.Handled = true;
        }

        void NetworkControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMouseCaptured)
            {
                Point point = e.GetPosition(this);

                double diffX = point.X - _scrollStartPoint.X;
                double diffY = point.Y - _scrollStartPoint.Y;

                _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - diffX);
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - diffY);

                _scrollStartPoint = point;

#if !SILVERLIGHT
                e.Handled = true;
#endif
            }
        }

        void NetworkControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isMouseCaptured)
            {
                gridZoom.ReleaseMouseCapture();
                _isMouseCaptured = false;
            }
            e.Handled = true;
            gridZoom.Cursor = Cursors.Arrow;
        }


        void NetworkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    sliderZoom.Value += sliderZoom.SmallChange;
                }
                else
                {
                    sliderZoom.Value -= sliderZoom.SmallChange;
                }

                e.Handled = true;
            }
        }

        #region Control Event Handlers
        void sliderRelevance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (KeyValuePair<object, NetworkVisualNode> kvp in _visualItems)
            {
                kvp.Value.UpdateRelevence(sliderRelevance.Value <= sliderRelevance.Minimum ? -1 : sliderRelevance.Value);
            }

            foreach (KeyValuePair<Line, KeyValuePair<NetworkVisualNode, NetworkVisualNode>> kvp in _connectors)
            {
                kvp.Key.Opacity = kvp.Value.Key.IsFiltered || kvp.Value.Value.IsFiltered ? 0.3 : 1;
            }
        }

        void sliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetZoom(sliderZoom.Value);
            //textZoom.Text = ((int)Math.Round(sliderZoom.Value * 100.0)).ToString() + "%";
        }

        void scrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((System.Windows.Input.Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    sliderZoom.Value += 0.1;
                }
                else
                {
                    sliderZoom.Value -= 0.1;
                }

                e.Handled = true;
            }
        }

#if SILVERLIGHT
        void _scrollViewer_LayoutUpdated(object sender, EventArgs e)
#else
        void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
#endif
        {
            double xPos;
            double yPos;

            if (_scrollViewer.ViewportWidth > _scrollViewer.ExtentWidth)
            {
                xPos = 0.5;
            }
            else
            {
                xPos = (_scrollViewer.HorizontalOffset + (_scrollViewer.ViewportWidth / 2)) / _scrollViewer.ExtentWidth;
            }

            if (_scrollViewer.ViewportHeight > _scrollViewer.ExtentHeight)
            {
                yPos = 0.5;
            }
            else
            {
                yPos = (_scrollViewer.VerticalOffset + (_scrollViewer.ViewportHeight / 2)) / _scrollViewer.ExtentHeight;
            }
            _scrollCentre = new Point(xPos, yPos);
        }
        #endregion

        #region Scroll
#if !SILVERLIGHT
        void TabletManager_HidScroll(bool isHorizontal, Obany.Hid.Tablet.Interop.SCROLLBARCOMMAND scrollBarCommand)
        {
            if (isHorizontal)
            {
                if (scrollBarCommand == Obany.Hid.Tablet.Interop.SCROLLBARCOMMAND.SB_LINELEFT)
                {
                    _scrollViewer.LineLeft();
                }
                else if (scrollBarCommand == Obany.Hid.Tablet.Interop.SCROLLBARCOMMAND.SB_LINERIGHT)
                {
                    _scrollViewer.LineRight();
                }
            }
        }
#endif
        #endregion

    }
}
