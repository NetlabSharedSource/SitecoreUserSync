﻿using System;
using System.Collections.Generic;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields
{
    /// <summary>
    /// this stores the plain text import value as is into the new field
    /// </summary>
    public class ToTextField : BaseMapping, IBaseField
    {
		#region Properties 

        /// <summary>
        /// name field delimiter
        /// </summary>
		public char[] comSplitr = { ',' };

		private IEnumerable<string> _existingDataNames;
		/// <summary>
		/// the existing data fields you want to import
		/// </summary>
        public IEnumerable<string> ExistingDataNames {
			get {
				return _existingDataNames;
			}
			set {
				_existingDataNames = value;
			}
		}

        private string _delimiter;
		/// <summary>
		/// the delimiter you want to separate imported data with
		/// </summary>
        public string Delimiter {
			get {
				return _delimiter;
			}
			set {
				_delimiter = value;
			}
		}
		
		#endregion Properties
		
		#region Constructor

        public ToTextField(BaseDataMap map, Item fieldItem): base(map, fieldItem)
        {
            //store fields
            ExistingDataNames = fieldItem.Fields["From What Fields"].Value.Split(comSplitr, StringSplitOptions.RemoveEmptyEntries);
			Delimiter = fieldItem.Fields["Delimiter"].Value;
        }

		#endregion Constructor
		
		#region Methods

        public string GetNewItemField()
        {
            return NewItemField;
        }

        #endregion Methods

        #region IBaseField Methods

        /// <summary>
        /// returns a string list of fields to import
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetExistingFieldNames()
        {
            return ExistingDataNames;
        }

        /// <summary>
        /// return the delimiter to separate imported values with
        /// </summary>
        /// <returns></returns>
        public string GetFieldValueDelimiter()
        {
            return Delimiter;
        }

        #endregion IBaseField Methods

        public override string ToString()
        {
            string info = "";
            info += "NewItemField: " + GetNewItemField();
            info += ". ExistingFieldNames: ";
            foreach (var existingFieldName in GetExistingFieldNames())
            {
                info += existingFieldName + "|";
            }
            info += ". FieldValueDelimiter: " + GetFieldValueDelimiter();
            return info;
        }
    }
}
