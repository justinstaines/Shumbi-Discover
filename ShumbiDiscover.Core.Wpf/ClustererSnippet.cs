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
using Obany.Cluster.Model;

namespace ShumbiDiscover.Core
{
    /// <summary>
    /// Class to represent a clustering service snippet
    /// </summary>
    public class ClustererSnippet : ISnippet
    {
        #region Members
        private Guid _id;
        private string _title;
        private string _body;
        private string _location;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ClustererSnippet()
        {
        }

        /// <summary>
        /// Value Constructor
        /// </summary>
        public ClustererSnippet(Guid id, string title, string body, string location)
        {
            _id = id;
            _title = title;
            _body = body;
            _location = location;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get or set the id
        /// </summary>
        public Guid Id
        {
            get
            {
                return (_id);
            }
            set
            {
                _id = value;
            }
        }
        #endregion

        #region ISnippet Properties
        /// <summary>
        /// Get the title for the snippet
        /// </summary>
        public string Title
        {
            get
            {
                return (_title);
            }
            set
            {
                _title = value;
            }
        }
        /// <summary>
        /// Get the body for the snippet
        /// </summary>
        public string Body
        {
            get
            {
                return (_body);
            }
            set
            {
                _body = value;
            }
        }

        /// <summary>
        /// Get the location for the snippet
        /// </summary>
        public string Location
        {
            get
            {
                return (_location);
            }
            set
            {
                _location = value;
            }
        }
        #endregion
    }
}