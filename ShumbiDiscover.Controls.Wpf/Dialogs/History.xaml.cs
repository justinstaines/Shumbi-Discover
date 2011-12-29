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
using System.Windows.Input;
using Obany.Core;
using Obany.UI;
using Obany.UI.Controls;
using ShumbiDiscover.Data;
using System.Windows.Media;

namespace ShumbiDiscover.Controls.Dialogs
{
    /// <summary>
    /// Class to display the history dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class History : UserControl, ILocalisable
    {
        #region Delegates
        /// <summary>
        /// Event handler definition for when an item is opened
        /// </summary>
        /// <param name="searchDescription">The selected search result set</param>
        public delegate void HistoryOpenItemDelegate(SearchDescription searchDescription);

        /// <summary>
        /// Event handler definition for when an item is deleted
        /// </summary>
        /// <param name="searchDescription">The entry to delete</param>
        public delegate void HistoryDeleteItemDelegate(SearchDescription searchDescription);

        /// <summary>
        /// Event handler definition for when the history is cleared
        /// </summary>
        public delegate void HistoryClearItemsDelegate();
        #endregion

        #region Fields
        private long _lastMouseDown;
        private object _lastMouseSender;
        #endregion

        #region Public Events
        /// <summary>
        /// Event called when an item is opened
        /// </summary>
        public event HistoryOpenItemDelegate HistoryOpenItem;
        /// <summary>
        /// Event called when items need deleting
        /// </summary>
        public event HistoryDeleteItemDelegate HistoryDeleteItem;
        /// <summary>
        /// Event called when all items clearing
        /// </summary>
        public event HistoryClearItemsDelegate HistoryClearItems;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public History()
        {
            InitializeComponent();

#if !SILVERLIGHT
            //listItems.MouseDoubleClick += new MouseButtonEventHandler(listItems_MouseDoubleClick);
#endif
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set the search history
        /// </summary>
        public List<SearchDescription> SearchHistory
        {
            set
            {
                foreach (SearchDescription searchDescription in value)
                {
                    listItems.Items.Add(searchDescription);
                }

                UpdateButtonStates();
            }
        }
        #endregion

        #region Event Handlers
        private void ListBoxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            long now = DateTime.Now.Ticks;

            if (now - _lastMouseDown < TimeSpan.FromMilliseconds(300).Ticks)
            {
                if (sender == _lastMouseSender)
                {
                    if (HistoryOpenItem != null)
                    {
                        HistoryOpenItem(listItems.SelectedItem as SearchDescription);
                    }
                }
            }

            _lastMouseSender = sender;
            _lastMouseDown = now;
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelHistoryQuery.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYSEARCH");
            labelHistoryDate.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYDATE");
            labelHistorySearchProviders.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYSEARCHENGINES");

            labelHistoryOpen.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYOPEN");
            labelHistoryDelete.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYDELETE");
            labelHistoryClear.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "HISTORYCLEAR");
        }

        #endregion

        #region Control Event Handlers
        private void buttonHistoryOpen_Click(object sender, RoutedEventArgs e)
        {
            if (listItems.SelectedItem != null)
            {
                if (HistoryOpenItem != null)
                {
                    HistoryOpenItem(listItems.SelectedItem as SearchDescription);
                }
            }
        }

        private void buttonHistoryDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listItems.SelectedItem != null)
            {
                DialogPanel.ShowQuestionBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "QUESTIONDELETEHISTORY"),
                                            DialogButtons.Yes | DialogButtons.No, 
                delegate(DialogResult dialogResult)
                {
                    if (dialogResult == DialogResult.Yes)
                    {
                        if (HistoryDeleteItem != null)
                        {
                            HistoryDeleteItem(listItems.SelectedItem as SearchDescription);
                        }
                        listItems.Items.Remove(listItems.SelectedItem);
                        UpdateButtonStates();
                    }
                });
            }
        }

        private void buttonHistoryClear_Click(object sender, RoutedEventArgs e)
        {
            DialogPanel.ShowQuestionBox(CultureHelper.GetString(Properties.Resources.ResourceManager, "QUESTIONCLEARHISTORY"),
                                        DialogButtons.Yes | DialogButtons.No,
            delegate(DialogResult dialogResult)
            {
                if (dialogResult == DialogResult.Yes)
                {
                    if (HistoryClearItems != null)
                    {
                        listItems.Items.Clear();
                        HistoryClearItems();
                    }
                    UpdateButtonStates();
                }
            });
        }

        private void listItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void listItems_MouseWheel(object sender, MouseWheelEventArgs e)
        {
#if SILVERLIGHT

            int newpos = listItems.SelectedIndex + (e.Delta > 0 ? -1 : 1);

            if (newpos >= 0 && newpos < listItems.Items.Count)
            {
                listItems.SelectedIndex = newpos;
            }

            e.Handled = true;
#endif
        }

        //void listItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    HitTestResult htr = VisualTreeHelper.HitTest(this, e.GetPosition(this));

        //    if(htr != null)
        //    {
        //        if(htr.VisualHit != null)
        //        {
        //            ListBoxItem lbi = htr.VisualHit.GetParent<ListBoxItem>();

        //            if(lbi != null)
        //            {
        //                if (HistoryOpenItem != null)
        //                {
        //                    HistoryOpenItem(lbi.Content as SearchDescription);
        //                }
        //            }
        //        }
        //    }
        //}

        #endregion

        #region Private Methids
        void UpdateButtonStates()
        {
            buttonHistoryDelete.IsEnabled = listItems.SelectedItem != null;
            buttonHistoryOpen.IsEnabled = listItems.SelectedItem != null;
            buttonHistoryClear.IsEnabled = listItems.Items.Count > 0;
        }
        #endregion


    }
}
