#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ShumbiDiscover.Model;
using System.Windows.Shapes;
using System.Windows.Media;

namespace ShumbiDiscover.Controls
{
    /// <summary>
    /// Control to show progress
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class ProgressControl : UserControl
    {
        #region Fields
        private Dictionary<string, ProgressBar> _progressItems;
        private Dictionary<string, Rectangle> _progressFinishedItems;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ProgressControl()
        {
            InitializeComponent();

            _progressItems = new Dictionary<string, ProgressBar>();
            _progressFinishedItems = new Dictionary<string, Rectangle>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set the satus
        /// </summary>
        public string Status
        {
            set
            {
                progressText.Text = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add an item to the progress
        /// </summary>
        /// <param name="itemName">The name of the item to add</param>
        /// <param name="maxProgress">Maximum value of progress</param>
        public void AddItem(string itemName, int maxProgress)
        {
            Grid g = new Grid();

            ColumnDefinition cd0 = new ColumnDefinition();
            cd0.Width = new GridLength(40, GridUnitType.Pixel);
            ColumnDefinition cd1 = new ColumnDefinition();
            cd1.Width = new GridLength(0.5, GridUnitType.Star);
            ColumnDefinition cd2 = new ColumnDefinition();
            cd2.Width = new GridLength(0.5, GridUnitType.Star);

            g.ColumnDefinitions.Add(cd0);
            g.ColumnDefinitions.Add(cd1);
            g.ColumnDefinitions.Add(cd2);

            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Center;
            image.Stretch = System.Windows.Media.Stretch.Uniform;
            image.Width = 32;
            image.Height = 32;
            image.Source = new System.Windows.Media.Imaging.BitmapImage(SearchProviderFactory.GetLargeImage(itemName));
            Grid.SetColumn(image, 0);

            TextBlock tbName = new TextBlock();
            tbName.Text = itemName;
            tbName.VerticalAlignment = VerticalAlignment.Center;
            tbName.FontSize = 16;
            Grid.SetColumn(tbName, 1);

            ProgressBar pbProgress = new ProgressBar();
            pbProgress.Minimum = 0;
            pbProgress.Maximum = maxProgress;
            pbProgress.Height = 30;
            Grid.SetColumn(pbProgress, 2);

            Rectangle rect = new Rectangle();
            rect.Fill = new SolidColorBrush(Color.FromArgb(0x2F, 0x00, 0xFF, 0x00));
            Grid.SetColumn(rect, 2);
            rect.Visibility = Visibility.Collapsed;
            rect.Margin = new Thickness(2);

            g.Children.Add(image);
            g.Children.Add(tbName);
            g.Children.Add(pbProgress);
            g.Children.Add(rect);
            g.Margin = new Thickness(0, 5, 0, 5);

            _progressItems.Add(itemName, pbProgress);
            _progressFinishedItems.Add(itemName, rect);
            progressItemsPanel.Children.Add(g);
        }

        /// <summary>
        /// Update an item in the progress
        /// </summary>
        /// <param name="itemName">The name of the item to update</param>
        /// <param name="progressValue">The progress value</param>
        public void UpdateItem(string itemName, int progressValue)
        {
            _progressItems[itemName].Value = progressValue;
            if (_progressItems[itemName].Value == _progressItems[itemName].Maximum)
            {
                _progressFinishedItems[itemName].Visibility = Visibility.Visible;
            }
        }
        #endregion
    }
}
