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
using System.Windows.Data;
using ShumbiDiscover.Data;

namespace ShumbiDiscover.Controls.Converters
{
    /// <summary>
    /// Class to tidy up the description from a search result
    /// </summary>
    public class SearchAggregateItemRankConverter : IValueConverter
    {
        #region Static Fields
        private static List<string> _providerFilters;
        #endregion

        #region Static Properties
        /// <summary>
        /// Set the provider filters
        /// </summary>
        public static List<string> ProviderFilters
        {
            set
            {
                _providerFilters = value;
            }
        }
        #endregion

        #region IValueConverter Members
        /// <summary>
        /// Convert the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Convert(value as List<SearchProviderRank>));
        }

        /// <summary>
        /// Convert the description
        /// </summary>
        /// <param name="searchProviderRanks">The value</param>
        /// <returns>Converted value</returns>
        public static string Convert(List<SearchProviderRank> searchProviderRanks)
        {
            return (SearchAggregateItem.CalculateRank(searchProviderRanks, _providerFilters).ToString());
        }

        /// <summary>
        /// Convert the value back
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
