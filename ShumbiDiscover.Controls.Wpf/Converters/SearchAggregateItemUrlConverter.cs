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
    public class SearchAggregateItemUrlConverter : IValueConverter
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
            return (Convert(value as string));
        }

        /// <summary>
        /// Convert the description
        /// </summary>
        /// <param name="res">The value</param>
        /// <returns>Converted value</returns>
        public static string Convert(string res)
        {
            if (!string.IsNullOrEmpty(res))
            {
                if (res.StartsWith("mapi:"))
                {
                    int startIdx = res.IndexOf("/0/");

                    if (startIdx > 0)
                    {
                        res = res.Substring(startIdx + 2);

                        int lastSlash = res.LastIndexOf("/");

                        if (lastSlash > 0)
                        {
                            res = res.Substring(0, lastSlash);
                        }
                    }
                    else
                    {
                        res = "Mail Item";
                    }
                }
            }
            return (res);
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
