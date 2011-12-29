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
    /// Control for login dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class ApiLogin : UserControl, ILocalisable
    {
        #region Delegates
        /// <summary>
        /// Event handler for when a url open is required
        /// </summary>
        /// <param name="url">The url to open</param>
        public delegate void OpenUrlRequestedEventHandler(Uri url);
        #endregion

        #region Fields
        private string _provider;
        private Uri _providerSignupUrl;
        #endregion

        #region Events
        /// <summary>
        /// Event call when a url open is requested
        /// </summary>
        public event OpenUrlRequestedEventHandler OpenUrlRequested;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ApiLogin()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set the provider
        /// </summary>
        public string Provider
        {
            set
            {
                _provider = value;
                labelProvider.Text = value;
            }
            get
            {
                return (_provider);
            }
        }

        /// <summary>
        /// Set the providericon
        /// </summary>
        public Uri ProviderIcon
        {
            set
            {
                if (value != null)
                {
                    imageProvider.Source = new System.Windows.Media.Imaging.BitmapImage(value);
                }
            }
        }

        /// <summary>
        /// Set the provider login information
        /// </summary>
        public string ProviderLoginInformation
        {
            set
            {
                labelLogin.Text = value;
            }
        }

        /// <summary>
        /// Set the provider signup information
        /// </summary>
        public string ProviderSignupInformation
        {
            set
            {
                labelSignupInformation.Text = value;
            }
        }

        /// <summary>
        /// Set the provider signup url
        /// </summary>
        public Uri ProviderSignupUrl
        {
            set
            {
                _providerSignupUrl = value;
            }
        }

        /// <summary>
        /// Get the api credentials 1
        /// </summary>
        public string ApiCredentials1
        {
            get
            {
                return (textEmailAddress.Text);
            }
        }

        /// <summary>
        /// Get the api credentials 2
        /// </summary>
        public string ApiCredentials2
        {
            get
            {
                return (textPassword.Password);
            }
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelEmailAddress.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "EMAILADDRESS");
            labelPassword.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORD");

            labelSignUpButton.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "SIGNUP");
        }
        #endregion

        #region Control Event Handlers
        private void buttonSignUp_Click(object sender, RoutedEventArgs e)
        {
            if(OpenUrlRequested != null)
            {
                OpenUrlRequested(_providerSignupUrl);
            }
        }
        #endregion
    }
}
