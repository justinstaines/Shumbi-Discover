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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Obany.UI;
using System.Windows.Threading;
using Obany.Core;

namespace ShumbiDiscover.Controls.Dialogs
{
    /// <summary>
    /// Control for login dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class Login : UserControl, ILocalisable
    {
        #region Delegates
        /// <summary>
        /// Event handler definition for login successful
        /// </summary>
        /// <param name="culture">The culture</param>
        /// <param name="authToken">The login token</param>
        public delegate void LoginSuccessDelegate(string culture, string authToken);
        #endregion

        #region Fields
        private string _cloudSessionId;
        #endregion

        #region Events
#if SILVERLIGHT
        /// <summary>
        /// Event called when login has been successful
        /// </summary>
        public event LoginSuccessDelegate LoginSuccess;
#endif
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Login()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Set the cloud session id
        /// </summary>
        public string CloudSessionId
        {
            set
            {
                _cloudSessionId = value;
            }
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelLogin.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LOGIN");
            labelEmailAddress.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "EMAILADDRESS");
            labelPassword.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "PASSWORD");
            labelButtonLogin.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LOGIN");
        }
        #endregion

        #region Control Event Handlers
        private void buttonLogin_Click(object sender, RoutedEventArgs e)
        {
#if SILVERLIGHT
            textEmailAddress.IsEnabled = false;
            textPassword.IsEnabled = false;
            buttonLogin.IsEnabled = false;

            Obany.Cloud.Services.Proxy.AuthenticationClient authenticationClient = Obany.Cloud.Client.Model.ClientFactory.CreateService<Obany.Cloud.Services.Proxy.AuthenticationClient>("Authentication");
            if (authenticationClient != null)
            {
                authenticationClient.LoginCompleted += new EventHandler<Obany.Cloud.Services.Proxy.LoginCompletedEventArgs>(authenticationClient_LoginCompleted);
                authenticationClient.LoginAsync(textEmailAddress.Text, textPassword.Password);
            }
#endif
        }

#if SILVERLIGHT
        void authenticationClient_LoginCompleted(object sender, Obany.Cloud.Services.Proxy.LoginCompletedEventArgs e)
        {
            bool success = false;

            try
            {
                if (e.Result.Status == Obany.Core.OperationStatus.Success)
                {
                    success = true;

                    if (LoginSuccess != null)
                    {
                        LoginSuccess(e.Result.Culture, e.Result.AuthToken);
                    }
                }
            }
            catch (Exception exc)
            {
                Obany.Core.Logging.LogException("Login::authenticationClient_LoginCompleted", exc, null, null, true);
            }

            if (success)
            {
                labelResult.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
                labelResult.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LOGINSUCCESS");
            }
            else
            {
                labelResult.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                labelResult.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "LOGINFAILED");
            }

            textEmailAddress.IsEnabled = true;
            textPassword.IsEnabled = true;
            buttonLogin.IsEnabled = true;
        }
#endif
        #endregion

    }
}
