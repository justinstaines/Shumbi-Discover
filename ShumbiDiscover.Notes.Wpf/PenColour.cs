#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System.Windows.Media;

namespace ShumbiDiscover.Notes
{
    /// <summary>
    /// Class to represent pen colour
    /// </summary>
    public class PenColour
    {
        #region Fields
        private string _name;
        private SolidColorBrush _brush;
        #endregion

        #region Constructors
        /// <summary>
        /// Value Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="a">Alpha</param>
        /// <param name="r">Red</param>
        /// <param name="g">Green</param>
        /// <param name="b">Blue</param>
        public PenColour(string name, byte a, byte r, byte g, byte b)
        {
            _name = name;
            _brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the name
        /// </summary>
        public string Name
        {
            get
            {
                return (_name);
            }
            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// Get or set the brush
        /// </summary>
        public SolidColorBrush Brush
        {
            get
            {
                return (_brush);
            }
            set
            {
                _brush = value;
            }
        }
        #endregion

        #region Object Overrride
        /// <summary>
        /// Convert the object to string representation
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            return(_name);
        }
        #endregion
    }
}
