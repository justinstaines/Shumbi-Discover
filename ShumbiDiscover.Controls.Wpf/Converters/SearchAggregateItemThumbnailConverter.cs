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
    public class SearchAggregateItemThumbnailConverter : IValueConverter
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
            object ret = null;
            string res = value as string;

            try
            {

#if SILVERLIGHT
                ret = res;
#else
                if (!string.IsNullOrEmpty(res))
                {
                    System.Windows.Media.Imaging.BitmapImage bs = new System.Windows.Media.Imaging.BitmapImage();
                    bs.BeginInit();
                    bs.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bs.UriSource = new Uri(res);
                    bs.EndInit();
                    ret = bs;
                }
#endif
            }
            catch
            {
            }

            return (ret);
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
