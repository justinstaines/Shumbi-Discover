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
    /// Control for document information dialog
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public partial class DocumentInformation : UserControl, ILocalisable
    {
        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public DocumentInformation()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the title
        /// </summary>
        public string Title
        {
            get
            {
                return (textTitle.Text);
            }
            set
            {
                textTitle.Text = value;
            }
        }

        /// <summary>
        /// Get or set the is public
        /// </summary>
        public bool IsPublic
        {
            get
            {
                return (checkIsPublic.IsChecked.Value);
            }
            set
            {
                checkIsPublic.IsChecked = value;
            }
        }

        /// <summary>
        /// Get or set the keywords
        /// </summary>
        public string Keywords
        {
            get
            {
                return (textKeywords.Text);
            }
            set
            {
                textKeywords.Text = value;
            }
        }
        #endregion

        #region ILocalisable Members
        /// <summary>
        /// Localise the object
        /// </summary>
        public void Localise()
        {
            labelTitle.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTTITLE");
            labelIsPublic.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTISPUBLIC");
            labelKeywordsInfo.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTKEYWORDSINFO");
            labelKeywords.Text = CultureHelper.GetString(Properties.Resources.ResourceManager, "DOCUMENTKEYWORDS");
        }
        #endregion
    }
}
