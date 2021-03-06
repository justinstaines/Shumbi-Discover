﻿#region Copyright Statement
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
using ShumbiDiscover.Model;

namespace ShumbiDiscover.Controls.Converters
{
    /// <summary>
    /// Class to display the best icon for an IFavouriteItem
    /// </summary>
    public class FavouriteIconConverter : IValueConverter
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
            string icon = "";

            if (value is Data.Favourite)
            {
                icon = VisualisationFactory.GetLargeImage((value as Data.Favourite).Visualisation).ToString();
            }
            else if (value is Data.FavouriteFolder)
            {
                icon = "../Resources/FavouriteFolder32.png";
            }

            return (icon);
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
