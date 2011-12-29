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

namespace ShumbiDiscover.Controls
{
    /// <summary>
    /// Control for selecting search source
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class VisualisationSelector : UserControl
    {
        #region Delegates
        /// <summary>
        /// Event handler called when the selection changes
        /// </summary>
        /// <param name="visualisation">The visualisation selected</param>
        public delegate void SelectionChangedEventHandler(string visualisation);
        #endregion

        #region Fields
        private WrapPanel panelButtons;
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
        public VisualisationSelector()
        {
            InitializeComponent();

            panelButtons = new WrapPanel();
            panelButtons.Orientation = Orientation.Horizontal;
            borderMain.Content = panelButtons;

            List<string> allVisualisations = VisualisationFactory.GetNames();

            for (int i = 0; i < allVisualisations.Count; i++)
            {
                ToggleButton button = new ToggleButton();
                button.Name = "button" + allVisualisations[i];
                button.Tag = allVisualisations[i];

                button.Width = 80;
                button.Height = 80;
                button.Click += new RoutedEventHandler(button_Click);

                StackPanel sp = new StackPanel();
                Image image = new Image();
                image.Stretch = System.Windows.Media.Stretch.Fill;
                image.Width = 48;
                image.Height = 48;
                image.Source = new BitmapImage(VisualisationFactory.GetLargeImage(allVisualisations[i]));

                TextBlock tb = new TextBlock();
                tb.Text = allVisualisations[i];
                tb.HorizontalAlignment = HorizontalAlignment.Center;

                sp.Children.Add(image);
                sp.Children.Add(tb);

                button.Content = sp;

                if (i == 0)
                {
                    button.IsChecked = true;
                }

                panelButtons.Children.Add(button);
            }

            int numItems = allVisualisations.Count;
            int columns = Math.Min(3, numItems);

            this.Width = (80 * columns) + 20;
            this.Height = (Math.Ceiling((double)numItems / (double)columns) * 80) + 20;

            this.MouseMove += new MouseEventHandler(VisualisationSelector_MouseMove);
            this.MouseLeave += new MouseEventHandler(VisualisationSelector_MouseLeave);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the visualisation
        /// </summary>
        public string Visualisation
        {
            get
            {
                string visualisations = "";

                foreach (ToggleButton button in panelButtons.Children)
                {
                    if (button.IsChecked.Value)
                    {
                        visualisations = (string)button.Tag;
                    }
                }

                return (visualisations);
            }
            set
            {

                foreach (ToggleButton button in panelButtons.Children)
                {
                    button.IsChecked = value == (string)button.Tag;
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
        void button_Click(object sender, RoutedEventArgs e)
        {
            foreach (ToggleButton button in panelButtons.Children)
            {
                button.IsChecked = button == sender;
            }

            if (SelectionChanged != null)
            {
                SelectionChanged(Visualisation);
            }
        }

        void VisualisationSelector_MouseMove(object sender, MouseEventArgs e)
        {
            ShowHide(true);
        }

        void VisualisationSelector_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowHide(false);
        }
        #endregion
    }
}
