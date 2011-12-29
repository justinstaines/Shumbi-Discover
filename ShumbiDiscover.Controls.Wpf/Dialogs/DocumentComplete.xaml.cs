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
using Obany.Core;
using Obany.UI;

namespace ShumbiDiscover.Controls.Dialogs
{
    /// <summary>
    /// Control for document complete dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class DocumentComplete : UserControl, ILocalisable
    {
        #region Delegates
        /// <summary>
        /// Event handler for when a url open is required
        /// </summary>
        /// <param name="url">The url to open</param>
        public delegate void OpenUrlRequestedEventHandler(Uri url);
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public DocumentComplete()
        {
            InitializeComponent();
        }
        #endregion

        #region Events
        /// <summary>
        /// Event call when a url open is requested
        /// </summary>
        public event OpenUrlRequestedEventHandler OpenUrlRequested;
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the url
        /// </summary>
        public string Url
        {
            get
            {
                return (textUrl.Text);
            }
            set
            {
                textUrl.Text = value;
            }
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelTitle.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTCOMPLETETITLE");
            labelUrl.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTCOMPLETEURL");
            labelOpenUrlButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTCOMPLETEOPENURL");
        }
        #endregion

        #region Event Handlers
        private void buttonOpenUrl_Click(object sender, RoutedEventArgs e)
        {
            if (OpenUrlRequested != null)
            {
                OpenUrlRequested(new Uri(textUrl.Text));
            }
        }
        #endregion
    }
}
