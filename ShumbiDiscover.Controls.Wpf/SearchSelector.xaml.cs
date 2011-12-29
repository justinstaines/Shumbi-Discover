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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ShumbiDiscover.Model;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Obany.Core;

namespace ShumbiDiscover.Controls
{
    /// <summary>
    /// Control for selecting search source
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class SearchSelector : UserControl
    {
        #region Delegates
        /// <summary>
        /// Event handler called when the selection changes
        /// </summary>
        /// <param name="searchProviders">The list of search providers</param>
        public delegate void SelectionChangedEventHandler(List<string> searchProviders);
        #endregion

        #region Fields
        private StackPanel panelGroups;
        private List<ToggleButton> _allButtons;
        private DispatcherTimer _hideTimer;
        #endregion

        #region Events
        /// <summary>
        /// Event called when selection changes
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public SearchSelector()
        {
            InitializeComponent();

            Repopulate();

            this.MouseMove += new MouseEventHandler(SearchSelector_MouseMove);
            this.MouseLeave += new MouseEventHandler(SearchSelector_MouseLeave);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Localise the control
        /// </summary>
        public void Localise()
        {
            List<string> searchProviders = SearchProviders;
            Repopulate();
            SearchProviders = searchProviders;
        }

        /// <summary>
        /// Get the current search provider selection
        /// </summary>
        /// <returns>List of selected search providers</returns>
        public List<string> SearchProviders
        {
            get
            {
                List<string> searchProviders = new List<string>();

                foreach (ToggleButton button in _allButtons)
                {
                    if(button.IsChecked.Value)
                    {
                        searchProviders.Add((string)button.Tag);
                    }
                }

                return (searchProviders);
            }
            set
            {

                foreach (ToggleButton button in _allButtons)
                {
                    string searchProvider = (string)button.Tag;

                    button.IsChecked = value.Contains(searchProvider);
                }
            }
        }

        /// <summary>
        /// Show or hide the selector
        /// </summary>
        /// <param name="showHide">Show or hide</param>
        public void ShowHide(bool showHide)
        {
            if (showHide)
            {
                this.Visibility = Visibility.Visible;

                if (_hideTimer != null)
                {
                    _hideTimer.Stop();
                    _hideTimer = null;
                }

                _hideTimer = new DispatcherTimer();
                _hideTimer.Interval = TimeSpan.FromSeconds(2);
                _hideTimer.Tick += delegate(object sender2, System.EventArgs e2)
                {
                    _hideTimer.Stop();
                    _hideTimer = null;

                    ShowHide(false);
                };
                _hideTimer.Start();
            }
            DoubleAnimation daOpacity = new DoubleAnimation();
            daOpacity.To = showHide ? 1 : 0;
            daOpacity.Duration = TimeSpan.FromSeconds(0.5);
            if (!showHide)
            {
                daOpacity.Completed += delegate(object sender, System.EventArgs e)
                {
                    this.Visibility = Visibility.Collapsed;
                };
            }
            this.BeginAnimation(OpacityProperty, daOpacity);
        }
        #endregion

        #region Control Event Handlers
        private void searchButton_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(SearchProviders);
            }
        }

        private void searchButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(SearchProviders);
            }
        }

        void SearchSelector_MouseMove(object sender, MouseEventArgs e)
        {
            ShowHide(true);
        }

        void SearchSelector_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowHide(false);
        }
        #endregion

        #region Private Methods
        private void Repopulate()
        {
            panelGroups = new StackPanel();
            panelGroups.Orientation = Orientation.Vertical;

            borderMain.Content = panelGroups;

            _allButtons = new List<ToggleButton>();

            List<string> allGroups = SearchProviderFactory.GetGroups();
            int maxGroupItems = 0;

            foreach (string group in allGroups)
            {
                TextBlock tbTitle = new TextBlock();
                tbTitle.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SEARCHGROUP" + group.ToUpper());
                tbTitle.FontSize = 14;

                panelGroups.Children.Add(tbTitle);

                WrapPanel panelButtons = new WrapPanel();
                panelButtons.Orientation = Orientation.Horizontal;

                panelGroups.Children.Add(panelButtons);

                List<string> typeNames = SearchProviderFactory.GetTypeNames(group);
                List<string> displayNames = SearchProviderFactory.GetDisplayNames(group);

                maxGroupItems = Math.Max(maxGroupItems, typeNames.Count);

                for (int i = 0; i < typeNames.Count; i++)
                {
                    ToggleButton button = new ToggleButton();
                    button.Tag = typeNames[i];

                    button.Width = 70;
                    button.Height = 70;
                    button.Checked += new RoutedEventHandler(searchButton_Checked);
                    button.Unchecked += new RoutedEventHandler(searchButton_Unchecked);

                    StackPanel sp = new StackPanel();
                    Image image = new Image();
                    image.Width = 40;
                    image.Height = 40;
                    image.Stretch = System.Windows.Media.Stretch.Fill;
                    image.Source = new BitmapImage(SearchProviderFactory.GetLargeImage(typeNames[i]));

                    TextBlock tb = new TextBlock();
                    tb.Text = displayNames[i];
                    tb.HorizontalAlignment = HorizontalAlignment.Center;

                    sp.Children.Add(image);
                    sp.Children.Add(tb);

                    button.Content = sp;

                    panelButtons.Children.Add(button);
                    _allButtons.Add(button);
                }

            }

            if (_allButtons.Count > 0)
            {
                _allButtons[0].IsChecked = true;
            }

            int columns = Math.Min(5, maxGroupItems);

            this.Width = (70 * columns) + 20;
            //this.Height = (Math.Ceiling((double)numItems / (double)columns) * 80) + 20;
        }
        #endregion
    }
}
