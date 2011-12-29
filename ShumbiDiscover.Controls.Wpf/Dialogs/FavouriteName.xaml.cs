#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Windows.Controls;
using Obany.Core;
using Obany.UI;

namespace ShumbiDiscover.Controls.Dialogs
{
    /// <summary>
    /// Control for configuration dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class FavouriteName : UserControl, ILocalisable
    {
        #region Fields
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public FavouriteName()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the name
        /// </summary>
        public string ItemName
        {
            get
            {
                return (textFavouriteName.Text);
            }
            set
            {
                textFavouriteName.Text = value;
            }
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelFavouriteName.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "FAVOURITENAME");
        }
        #endregion
    }
}
