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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShumbiDiscover.Model;
using System.Collections.Generic;

namespace ShumbiDiscover.Controls
{
    /// <summary>
    /// Control to display search visualisation tabs
    /// </summary>
    public partial class SearchVisualisationTab : UserControl
    {
        #region Dependency Properties
        /// <summary>
        /// The brush for painting the glyphs
        /// </summary>
        public static readonly DependencyProperty GlyphBrushProperty = DependencyProperty.Register("GlyphBrush", typeof(Brush), typeof(SearchVisualisationTab), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00)), OnGlyphBrushChanged));
        #endregion

        #region Delegates
        /// <summary>
        /// Event handler used for search visualisation selection
        /// </summary>
        /// <param name="searchVisualisation">The search visualisation being selected</param>
        public delegate void ItemSelectedEventHandler(SearchVisualisation searchVisualisation);

        /// <summary>
        /// Event handler used for search visualisation is added to favourites
        /// </summary>
        /// <param name="searchVisualisation">The search visualisation being added to favourites</param>
        public delegate void AddToFavouritesEventHandler(SearchVisualisation searchVisualisation);
        #endregion

        #region Fields
        private int _currentIndex;
        #endregion

        #region Events
        /// <summary>
        /// Event called when search visualisation is selected
        /// </summary>
        public event ItemSelectedEventHandler ItemSelected;
        /// <summary>
        /// Event called when search visualisation is added to favourites
        /// </summary>
        public event AddToFavouritesEventHandler AddToFavourites;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public SearchVisualisationTab()
        {
            InitializeComponent();

            _currentIndex = -1;

            this.SizeChanged += new SizeChangedEventHandler(SearchVisualisationTab_SizeChanged);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the glyph brush
        /// </summary>
        public Brush GlyphBrush
        {
            get
            {
                return (Brush)base.GetValue(GlyphBrushProperty);
            }
            set
            {
                base.SetValue(GlyphBrushProperty, (DependencyObject)value);
            }
        }
        #endregion

        #region DependencyProperty Changed Handlers
        private static void OnGlyphBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SearchVisualisationTab source = d as SearchVisualisationTab;
            source.glyphLeft.Fill = source.GlyphBrush;
            source.glyphRight.Fill = source.GlyphBrush;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a new search visualisation
        /// </summary>
        /// <param name="searchVisualisation">The search visualisation to add</param>
        public void AddSearchVisualisation(SearchVisualisation searchVisualisation)
        {
            ToggleButton button = CreateTabButton(searchVisualisation);

            _items.Children.Add(button);

            SelectNewItem(button);

            _currentIndex = _items.Children.IndexOf(button);

            CalculateButtonVisibility(true);
        }

        /// <summary>
        /// Remove a search visualisation from the list
        /// </summary>
        /// <param name="searchVisualisation">The search visualisation to remove</param>
        public void RemoveSearchVisualisation(SearchVisualisation searchVisualisation)
        {
            int found = -1;
            for (int i = 0; i < _items.Children.Count && found == -1; i++)
            {
                ToggleButton button = _items.Children[i] as ToggleButton;

                if (button.Tag == searchVisualisation)
                {
                    found = i;
                    _items.Children.RemoveAt(i);
                    if (i <= _currentIndex)
                    {
                        _currentIndex--;
                    }
                }
            }

            if (found != -1)
            {
                int newIndex = _currentIndex;

                if (newIndex < 0)
                {
                    newIndex = 0;
                }

                if (ItemSelected != null)
                {
                    if (newIndex < _items.Children.Count)
                    {
                        ToggleButton b = _items.Children[newIndex] as ToggleButton;

                        SelectNewItem(b);
                    }
                    else
                    {
                        ItemSelected(null);
                    }
                }
            }

            CalculateButtonVisibility(true);
        }

        /// <summary>
        /// Select the search visualisation
        /// </summary>
        /// <param name="id">The id of the search visualisation to select</param>
        /// <returns>True if an item was selected</returns>
        public bool SelectSearchVisualisation(Guid id)
        {
            bool found = false;
            for (int i = 0; i < _items.Children.Count && !found; i++)
            {
                ToggleButton b = _items.Children[i] as ToggleButton;

                SearchVisualisation searchVisualisation = b.Tag as SearchVisualisation;

                if (searchVisualisation != null)
                {
                    if (searchVisualisation.SearchResultSet.SearchDescription.Id == id)
                    {
                        found = true;
                        SelectNewItem(b);
                    }
                }
            }

            return (found);
        }

        /// <summary>
        /// Find the search visualisation
        /// </summary>
        /// <param name="id">The id of the item to find</param>
        public SearchVisualisation FindSearchVisualisation(Guid id)
        {
            SearchVisualisation found = null;
            for (int i = 0; i < _items.Children.Count && found == null; i++)
            {
                ToggleButton b = _items.Children[i] as ToggleButton;

                SearchVisualisation searchVisualisation = b.Tag as SearchVisualisation;

                if(searchVisualisation != null)
                {
                    if (searchVisualisation.SearchResultSet.SearchDescription.Id == id)
                    {
                        found = searchVisualisation;
                    }
                }
            }

            return (found);
        }

        /// <summary>
        /// Refresh the search visualisation
        /// </summary>
        /// <param name="refreshSearchVisualisation">The search visualisation to refresh</param>
        public void RefreshSearchVisualisation(SearchVisualisation refreshSearchVisualisation)
        {
            bool found = false;
            for (int i = 0; i < _items.Children.Count && !found; i++)
            {
                ToggleButton b = _items.Children[i] as ToggleButton;

                SearchVisualisation searchVisualisation = b.Tag as SearchVisualisation;

                if (searchVisualisation != null)
                {
                    if (searchVisualisation.SearchResultSet.SearchDescription.Id == refreshSearchVisualisation.SearchResultSet.SearchDescription.Id)
                    {
                        found = true;

                        Grid g = b.Content as Grid;

                        if (g != null)
                        {
                            Image image = g.Children[0] as Image;

                            if (image != null)
                            {
                                image.Source = new System.Windows.Media.Imaging.BitmapImage(VisualisationFactory.GetSmallImage(searchVisualisation.Visualisation));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Localise the control
        /// </summary>
        public void Localise()
        {
            for (int i = 0; i < _items.Children.Count; i++)
            {
                ToggleButton b = _items.Children[i] as ToggleButton;

                SearchVisualisation searchVisualisation = b.Tag as SearchVisualisation;

                if (searchVisualisation != null)
                {
                    searchVisualisation.Localise();
                }
            }
        }
        #endregion

        #region Control Overrides
        /// <summary>
        /// Handle the size changed event
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The size info</param>
        void SearchVisualisationTab_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateButtonVisibility(true);
        }
        #endregion

        #region Private Method
        internal void CalculateButtonVisibility(bool startFromLeft)
        {
            double widthSoFar = 0;
            int visibleCount = 0;
            List<double> measuredWidths = new List<double>();

            bool outOfSpace = false;

            widthSoFar = buttonLeft.Width + buttonRight.Width; // Leave space for the buttons

            if (_items.Children.Count > 0)
            {
                // First Measure all the items
                for (int i = 0; i < _items.Children.Count; i++)
                {
                    ToggleButton tabButton = _items.Children[i] as ToggleButton;

                    tabButton.Visibility = Visibility.Visible;
                    measuredWidths.Add(tabButton.Width);
                    tabButton.Visibility = Visibility.Collapsed;

                    if (_currentIndex == i)
                    {
                        widthSoFar += measuredWidths[i];
                        if (widthSoFar <= this.ActualWidth)
                        {
                            tabButton.Visibility = Visibility.Visible;
                            visibleCount++;
                        }
                        else
                        {
                            outOfSpace = true;
                        }
                    }
                }

                int leftIndex = _currentIndex - 1;
                int rightIndex = _currentIndex + 1;

                bool outOfItems = false;

                while (!outOfSpace && !outOfItems)
                {
                    int testIndex = -1;

                    bool tryLeft = startFromLeft;
                    while (!outOfItems && testIndex == -1)
                    {
                        if (startFromLeft)
                        {
                            if (tryLeft)
                            {
                                if (leftIndex >= 0)
                                {
                                    testIndex = leftIndex;
                                    leftIndex--;
                                }
                                else
                                {
                                    tryLeft = false;
                                }
                            }

                            if (!tryLeft)
                            {
                                if (rightIndex < _items.Children.Count)
                                {
                                    testIndex = rightIndex;
                                    rightIndex++;
                                }
                            }
                        }
                        else
                        {
                            if (!tryLeft)
                            {
                                if (rightIndex < _items.Children.Count)
                                {
                                    testIndex = rightIndex;
                                    rightIndex++;
                                }
                                else
                                {
                                    tryLeft = true;
                                }
                            }

                            if (tryLeft)
                            {
                                if (leftIndex >= 0)
                                {
                                    testIndex = leftIndex;
                                    leftIndex--;
                                }
                            }
                        }

                        if (testIndex == -1)
                        {
                            outOfItems = true;
                        }

                        tryLeft = !tryLeft;
                    }

                    if (!outOfItems)
                    {
                        widthSoFar += measuredWidths[testIndex];
                        if (widthSoFar <= this.ActualWidth)
                        {
                            _items.Children[testIndex].Visibility = Visibility.Visible;
                            visibleCount++;
                        }
                        else
                        {
                            outOfSpace = true;
                        }
                    }
                }
            }
            int firstVisible = FindFirstVisible();
            int lastVisible = FindLastVisible();

            buttonLeft.Visibility = Visibility.Collapsed;
            buttonRight.Visibility = Visibility.Collapsed;
            if (firstVisible > 0)
            {
                buttonLeft.IsEnabled = true;
                buttonLeft.Visibility = Visibility.Visible;
                buttonRight.Visibility = Visibility.Visible;
            }
            else
            {
                buttonLeft.IsEnabled = false;
            }
            if (lastVisible < _items.Children.Count - 1 && lastVisible != -1)
            {
                buttonRight.IsEnabled = true;
                buttonLeft.Visibility = Visibility.Visible;
                buttonRight.Visibility = Visibility.Visible;
            }
            else
            {
                buttonRight.IsEnabled = false;
            }
        }

        private int FindFirstVisible()
        {
            int firstVisible = -1;

            for (int i = 0; i < _items.Children.Count && firstVisible == -1; i++)
            {
                if (_items.Children[i].Visibility == Visibility.Visible)
                {
                    firstVisible = i;
                }
            }
            return (firstVisible);
        }

        private int FindLastVisible()
        {
            int lastVisible = -1;

            for (int i = _items.Children.Count - 1; i >= 0 && lastVisible == -1; i--)
            {
                if (_items.Children[i].Visibility == Visibility.Visible)
                {
                    lastVisible = i;
                }
            }
            return (lastVisible);
        }


        private ToggleButton CreateTabButton(SearchVisualisation searchVisualisation)
        {
            ToggleButton button = new ToggleButton();

            Grid g = new Grid();

            ColumnDefinition cd0 = new ColumnDefinition();
            cd0.Width = new GridLength(6);

            ColumnDefinition cd1 = new ColumnDefinition();
            cd1.Width = new GridLength(20);

            ColumnDefinition cd2 = new ColumnDefinition();
            cd2.Width = new GridLength(6);

            ColumnDefinition cd3 = new ColumnDefinition();
            cd3.Width = new GridLength(80);

            ColumnDefinition cd4 = new ColumnDefinition();
            cd4.Width = new GridLength(6);

            ColumnDefinition cd5 = new ColumnDefinition();
            cd5.Width = new GridLength(20);

            ColumnDefinition cd6 = new ColumnDefinition();
            cd6.Width = new GridLength(20);

            ColumnDefinition cd7 = new ColumnDefinition();
            cd7.Width = new GridLength(6);

            g.ColumnDefinitions.Add(cd0);
            g.ColumnDefinitions.Add(cd1);
            g.ColumnDefinitions.Add(cd2);
            g.ColumnDefinitions.Add(cd3);
            g.ColumnDefinitions.Add(cd4);
            g.ColumnDefinitions.Add(cd5);
            g.ColumnDefinitions.Add(cd6);
            g.ColumnDefinitions.Add(cd7);

            Image image = new Image();
            image.Width = 20;
            image.Height = 20;
            image.Source = new System.Windows.Media.Imaging.BitmapImage(VisualisationFactory.GetSmallImage(searchVisualisation.Visualisation));
            Grid.SetColumn(image, 1);
            g.Children.Add(image);

            TextBlock tb = new TextBlock();
            tb.Text = searchVisualisation.SearchResultSet.SearchDescription.Query;
            tb.TextWrapping = TextWrapping.NoWrap;
#if !SILVERLIGHT
            tb.TextTrimming = TextTrimming.CharacterEllipsis;
#endif
            tb.TextAlignment = TextAlignment.Left;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(tb, 3);
            g.Children.Add(tb);

            Button favouritesAddButton = new Button();
            favouritesAddButton.Click += new RoutedEventHandler(favouritesAddButton_Click);
            favouritesAddButton.Width = 20;
            favouritesAddButton.Height = 20;
            favouritesAddButton.Tag = searchVisualisation;

            Image imageFavouritesAdd = new Image();
            imageFavouritesAdd.Source = new BitmapImage(new Uri("Resources/FavouritesAdd16.png", UriKind.Relative));
            imageFavouritesAdd.Stretch = Stretch.None;

            favouritesAddButton.Content = imageFavouritesAdd;

            Grid.SetColumn(favouritesAddButton, 5);
            g.Children.Add(favouritesAddButton);

            TextBlock tbClose = new TextBlock();
            tbClose.Text = "X";
            tbClose.VerticalAlignment = VerticalAlignment.Top;

            Button closeButton = new Button();
            closeButton.Margin = new Thickness(0, 0, 0, 0);
            closeButton.Click += new RoutedEventHandler(closeButton_Click);
            closeButton.Content = tbClose;
            closeButton.Tag = searchVisualisation;
            closeButton.Width = 20;
            closeButton.Height = 20;

            Grid.SetColumn(closeButton, 6);
            g.Children.Add(closeButton);

            g.Tag = searchVisualisation;
            g.Width = 164;
            g.Height = 32;

            button.Content = g;
            button.Tag = searchVisualisation;
            button.Height = 32;
            button.Width = 164;

            button.Click += new RoutedEventHandler(button_Click);

            return(button);
        }

        void favouritesAddButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                if (AddToFavourites != null)
                {
                    AddToFavourites(button.Tag as SearchVisualisation);
                }
            }
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = _items.Children.IndexOf(sender as ToggleButton);
            SelectNewItem(sender as ToggleButton);
        }
        #endregion

        #region Control Event Handlers
        private void SelectNewItem(ToggleButton button)
        {
            foreach (ToggleButton b in _items.Children)
            {
                if (b == button)
                {
                    b.IsChecked = true;
                }
                else
                {
                    b.IsChecked = false;
                }
            }

            if (ItemSelected != null)
            {
                ItemSelected(button.Tag as SearchVisualisation);
            }
        }

        void closeButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSearchVisualisation((sender as Button).Tag as SearchVisualisation);
#if !SILVERLIGHT
            e.Handled = true;
#endif
        }

        private void buttonLeft_Click(object sender, RoutedEventArgs e)
        {
            int firstVisible = FindFirstVisible();
            if (firstVisible > 0)
            {
                _currentIndex = firstVisible - 1;
                CalculateButtonVisibility(false);
            }
        }

        private void buttonRight_Click(object sender, RoutedEventArgs e)
        {
            int lastVisible = FindLastVisible();
            if (lastVisible < _items.Children.Count - 1)
            {
                _currentIndex = lastVisible + 1;
                CalculateButtonVisibility(true);
            }
        }
        #endregion
    }
}
