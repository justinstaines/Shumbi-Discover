#region Copyright Statement
// Copyright © 2009 Shumbi Ltd.
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion

namespace ShumbiDiscover.Notes
{
    /// <summary>
    /// Class to represent pen thickness
    /// </summary>
    public class PenThickness
    {
        #region Fields
        private string _name;
        private double _thickness;
        #endregion

        #region Constructors
        /// <summary>
        /// Value Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="thickness"></param>
        public PenThickness(string name, double thickness)
        {
            _name = name;
            _thickness = thickness;
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
        /// Get or set the thickness
        /// </summary>
        public double Thickness 
        {
            get
            {
                return (_thickness);
            }
            set
            {
                _thickness = value;
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
