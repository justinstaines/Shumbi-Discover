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
using System.Windows.Data;

namespace ShumbiDiscover.Controls.Converters
{
    /// <summary>
    /// Class to tidy up the description from a search result
    /// </summary>
    public class SearchAggregateItemDescriptionConverter : IValueConverter
    {
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
            return (Convert(value as string, 200));
        }

        /// <summary>
        /// Convert the description
        /// </summary>
        /// <param name="res">The value</param>
        /// <param name="maxLength">The max length of the description, 0 for all of it</param>
        /// <returns>Converted value</returns>
        public static string Convert(string res, int maxLength)
        {
            if (!string.IsNullOrEmpty(res))
            {
                res = Obany.Communications.HtmlHelper.Cleanup(res);

                if (maxLength > 0)
                {
                    res = res.Replace("\n", " ");
                    res = res.Replace("\r", "");

                    if (res.Length > maxLength)
                    {
                        res = res.Substring(0, maxLength) + "...";
                    }
                }
            }
            return(res);
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
