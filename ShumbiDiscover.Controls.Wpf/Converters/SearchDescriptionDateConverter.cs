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
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ShumbiDiscover.Controls.Converters
{
    /// <summary>
    /// Class to tidy up the description from a search result
    /// </summary>
    public class SearchDescriptionDateConverter : IValueConverter
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
            string res = "";

            DateTime dt = (DateTime)value;

            if(dt != null)
            {
                res = dt.ToString("g");
            }
            return (res);
        }

        /// <summary>
        /// Convert the description
        /// </summary>
        /// <param name="res">The value</param>
        /// <returns>Converted value</returns>
        public static string ConvertDescription(string res)
        {
            if (!string.IsNullOrEmpty(res))
            {
                res = Regex.Replace(res, @"<(.|\n)*?>", string.Empty);
                res = Regex.Replace(res, @"&lt;", "<");
                res = Regex.Replace(res, @"&gt;", ">");
                res = Regex.Replace(res, @"&quot;", "'");
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
