#region Copyright Statement
// Copyright © 2009 Shumbi Ltd
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Windows;
using ShumbiDiscover.Data;

namespace ShumbiDiscover.Controls.EventArgs
{
    /// <summary>
    /// Event args for selection value changed
    /// </summary>
    public class SelectionChangedEventArgs : RoutedEventArgs
    {
        #region Fields
        private SearchAggregateItem _newValue;
        #endregion

        #region Constructors
        /// <summary>
        /// Value Constructor
        /// </summary>
        /// <param name="newValue">The new value</param>
        public SelectionChangedEventArgs(SearchAggregateItem newValue)
        {
            _newValue = newValue;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the new value
        /// </summary>
        public SearchAggregateItem NewValue
        {
            get
            {
                return (_newValue);
            }
        }
        #endregion
    }
}
