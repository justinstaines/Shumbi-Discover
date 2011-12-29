#region Copyright Statement
// Copyright © 2009 Shumbi Ltd
//
// All rights are reserved. Reproduction or transmission in whole or
// in part, in any form or by any means, electronic, mechanical or
// otherwise, is prohibited without the prior written consent
// of the copyright owner.
//
#endregion
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ShumbiDiscover
{
    /// <summary>
    /// Class to represent an update
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public class Update
    {
        #region Fields
        private string _productName;
        private string _version;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public Update()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the product name
        /// </summary>
        [XmlElement(ElementName = "ProductName")]
        public string ProductName
        {
            get
            {
                return (_productName);
            }
            set
            {
                _productName = value;
            }
        }

        /// <summary>
        /// Get or set the version
        /// </summary>
        [XmlElement(ElementName = "Version")]
        public string Version
        {
            get
            {
                return (_version);
            }
            set
            {
                _version = value;
            }
        }
        #endregion
    }
}
