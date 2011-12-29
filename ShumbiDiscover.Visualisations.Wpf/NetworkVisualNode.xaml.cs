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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ShumbiDiscover.Model;

namespace ShumbiDiscover.Visualisations
{
    /// <summary>
    /// Class to handle a visual network node
    /// </summary>
    public partial class NetworkVisualNode : UserControl
    {
        #region Dependency Properties
        /// <summary>
        /// The brush for selected colour
        /// </summary>
        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register("SelectedBrush", typeof(Brush), typeof(NetworkVisualNode), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)), OnSelectedBrushChanged));
        #endregion

        #region Delegates
        /// <summary>
        /// The event handler definition for when a node is selected
        /// </summary>
        /// <param name="node">The node being selected</param>
        public delegate void NodeSelectedEventHandler(NetworkVisualNode node);
        /// <summary>
        /// The event handler definition for  when a node is activated
        /// </summary>
        /// <param name="node">The node being activated</param>
        public delegate void NodeActivatedEventHandler(NetworkVisualNode node);
        /// <summary>
        /// The event handler definition for  when a node is hovered
        /// </summary>
        /// <param name="node">The node being hovered</param>
        public delegate void NodeHoveredEventHandler(NetworkVisualNode node);
        #endregion

        #region Fields
        private object _contentObject;
        private bool _isHovered;
        private bool _isSelected;
        private bool _isFiltered;
        private bool _isExpanded;

        private ScaleTransform _scaleTransform;

        private long _lastMouseUp;
        #endregion

        #region Events
        /// <summary>
        /// Event to fire when a node is selected
        /// </summary>
        public event NodeSelectedEventHandler NodeSelected;
        /// <summary>
        /// Event to fire when a node is activated
        /// </summary>
        public event NodeActivatedEventHandler NodeActivated;
        /// <summary>
        /// Event to fire when a node is hovered
        /// </summary>
        public event NodeHoveredEventHandler NodeHovered;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public NetworkVisualNode()
        {
            InitializeComponent();

            _isHovered = false;
            _isSelected = false;
            _isFiltered = false;
            _isExpanded = false;

            _scaleTransform = new ScaleTransform();
            this.RenderTransform = _scaleTransform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            base.MouseEnter += new MouseEventHandler(NetworkVisualNode_MouseEnter);
            base.MouseLeave += new MouseEventHandler(NetworkVisualNode_MouseLeave);
            base.MouseLeftButtonDown += new MouseButtonEventHandler(NetworkVisualNode_MouseLeftButtonDown);

            ChildMarker.Visibility = Visibility.Collapsed;
            rectangleSelected.Opacity = 0;
            _lastMouseUp = 0;
            NodeSelected = null;
            NodeActivated = null;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the selected brush
        /// </summary>
        public Brush SelectedBrush
        {
            get
            {
                return (Brush)base.GetValue(SelectedBrushProperty);
            }
            set
            {
                base.SetValue(SelectedBrushProperty, (DependencyObject)value);
            }
        }

        /// <summary>
        /// Get or set the Network Node
        /// </summary>
        public object ContentObject
        {
            get
            {
                return (_contentObject);
            }
            set
            {
                _contentObject = value;
                _name.Text = _contentObject.ToString();
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Get or Set the selected flag
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return (_isSelected);
            }
            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Get or Set the filtered flag
        /// </summary>
        public bool IsFiltered
        {
            get
            {
                return (_isFiltered);
            }
            set
            {
                _isFiltered = value;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Is the node expanded
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return (_isExpanded);
            }
            set
            {
                _isExpanded = value;
            }
        }


        /// <summary>
        /// Get or set the centre of the item
        /// </summary>
        public Point Centre
        {
            set
            {
                Point location = new Point(value.X - this.ActualWidth / 2, value.Y - this.ActualHeight / 2);

                Canvas.SetLeft(this, location.X);
                Canvas.SetTop(this, location.Y);
            }
            get
            {
                Point location = new Point(Canvas.GetLeft(this) + this.ActualWidth / 2, Canvas.GetTop(this) + this.ActualHeight / 2);

                return (location);
            }
        }
        #endregion

        #region Depenency Property Events
        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NetworkVisualNode source = d as NetworkVisualNode;
            source.rectangleSelected.Fill = source.SelectedBrush;
        }
        #endregion

        #region Control Event Handlers
        void NetworkVisualNode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            long now = DateTime.Now.Ticks;

            if (NodeSelected != null)
            {
                NodeSelected(this);
            }

            if (now - _lastMouseUp < TimeSpan.FromMilliseconds(300).Ticks)
            {
                if (NodeActivated != null)
                {
                    NodeActivated(this);
                }
            }

            _lastMouseUp = now;

            e.Handled = true;
        }

        void NetworkVisualNode_MouseLeave(object sender, MouseEventArgs e)
        {
            _isHovered = false;
            UpdateVisualState();
        }

        void NetworkVisualNode_MouseEnter(object sender, MouseEventArgs e)
        {
            _isHovered = true;
            UpdateVisualState();
            if (NodeHovered != null)
            {
                NodeHovered(this);
            }
        }
        #endregion

        #region Private Methods
        void UpdateVisualState()
        {
            UpdateOpacity();
            DoHoverAnimation();

            rectangleSelected.Opacity = _isSelected ? 1 : 0;
            ICollection collection = _contentObject as ICollection;
            int childCount = 0;
            if (collection != null)
            {
                childCount = collection.Count;
            }
            ChildMarker.Visibility = childCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        internal void UpdateOpacity()
        {
            double newOpacity = _isFiltered ? 0.3 : 1;

            DoubleAnimation da = new DoubleAnimation();
            da.To = newOpacity;
            da.Duration = TimeSpan.FromMilliseconds(300);

            this.BeginAnimation(OpacityProperty, da);
        }

        internal void DoHoverAnimation()
        {
            double destScale = 1;

            if (_isHovered)
            {
                destScale = 1.4;
            }

            DoubleAnimation scaleAnimX = new DoubleAnimation();
            scaleAnimX.To = destScale;
            scaleAnimX.Duration = TimeSpan.FromMilliseconds(300);

            DoubleAnimation scaleAnimY = new DoubleAnimation();
            scaleAnimY.To = destScale;
            scaleAnimY.Duration = TimeSpan.FromMilliseconds(300);

            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimX);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimY);

            if (destScale == 1)
            {
                ApplyLabel(true);
            }
            else
            {
                ApplyLabel(false);
            }
        }

        void ApplyLabel(bool shortForm)
        {
            string title = _contentObject.ToString();
            if (title.Length > 20 && shortForm)
            {
                title = title.Substring(0, 20) + "...";
            }
            _name.Text = title;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the visual state based on the relevance limit
        /// </summary>
        /// <param name="relevanceLimit">The relevance limit</param>
        public void UpdateRelevence(double relevanceLimit)
        {
            double relval = 0;
            IRelevance relevance = _contentObject as IRelevance;
            if (relevance != null)
            {
                relval = relevance.Relevance;
            }
            _isFiltered = relval < relevanceLimit;
            UpdateVisualState();
        }
        #endregion
    }
}
