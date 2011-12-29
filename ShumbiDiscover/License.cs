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
    /// Class to represent a license
    /// </summary>
    [SmartAssembly.Attributes.DoNotObfuscate]
    public class License
    {
        #region Fields
        private string _os;
        private string _osVersion;
        private string _servicePack;
        private string _applicationName;
        private string _applicationVersion;
        private DateTime _dateTimeUtc;
        private string _processorId;
        private string _biosSerial;
        private string _oemHardwareId;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public License()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the os
        /// </summary>
        [XmlElement(ElementName = "OS")]
        public string OS
        {
            get
            {
                return (_os);
            }
            set
            {
                _os = value;
            }
        }

        /// <summary>
        /// Get or set the OS version
        /// </summary>
        [XmlElement(ElementName = "OSVersion")]
        public string OSVersion
        {
            get
            {
                return (_osVersion);
            }
            set
            {
                _osVersion = value;
            }
        }

        /// <summary>
        /// Get or set the service pack
        /// </summary>
        [XmlElement(ElementName = "ServicePack")]
        public string ServicePack
        {
            get
            {
                return (_servicePack);
            }
            set
            {
                _servicePack = value;
            }
        }

        /// <summary>
        /// Get or set the application name
        /// </summary>
        [XmlElement(ElementName="ApplicationName")]
        public string ApplicationName
        {
            get
            {
                return (_applicationName);
            }
            set
            {
                _applicationName = value;
            }
        }

        /// <summary>
        /// Get or set the application version
        /// </summary>
        [XmlElement(ElementName = "ApplicationVersion")]
        public string ApplicationVersion
        {
            get
            {
                return (_applicationVersion);
            }
            set
            {
                _applicationVersion = value;
            }
        }

        /// <summary>
        /// Get or set the date time utc
        /// </summary>
        [XmlElement(ElementName = "DateTimeUtc")]
        public DateTime DateTimeUtc
        {
            get
            {
                return (_dateTimeUtc);
            }
            set
            {
                _dateTimeUtc = value;
            }
        }

        /// <summary>
        /// Get or set the processor id
        /// </summary>
        [XmlElement(ElementName = "ProcessorID")]
        public string ProcessorId
        {
            get
            {
                return (_processorId);
            }
            set
            {
                _processorId = value;
            }
        }

        /// <summary>
        /// Get or set the hard bios serial
        /// </summary>
        [XmlElement(ElementName = "BIOSSerial")]
        public string BiosSerial
        {
            get
            {
                return (_biosSerial);
            }
            set
            {
                _biosSerial = value;
            }
        }

        /// <summary>
        /// Get or set the oem hardware id
        /// </summary>
        [XmlElement(ElementName = "OEMHardwareID")]
        public string OemHardwareId
        {
            get
            {
                return (_oemHardwareId);
            }
            set
            {
                _oemHardwareId = value;
            }
        }
        #endregion
    }
}
