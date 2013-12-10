using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Providers;
using Sitecore.SharedSource.UserSync.Utility;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields
{
    /// <summary>
    /// This uses imported values to match by name an existing content item in the list provided
    /// then stores the GUID of the existing item
    /// </summary>
    public class ToGuidFromListValueMatchOnFieldNameFieldField : ToGuidFromListValueMatchOnDisplayNameField
    {
        #region Properties

        private string _MatchOnFieldName;
        /// <summary>
        /// This is the list that you will compare the imported values against
        /// </summary>
        public string MatchOnFieldName
        {
            get
            {
                return _MatchOnFieldName;
            }
            set
            {
                _MatchOnFieldName = value;
            }
        }

        #endregion Properties

        #region Constructor

        public ToGuidFromListValueMatchOnFieldNameFieldField(BaseDataMap map, Item fieldItem)
            : base(map, fieldItem)
        {
            // Stores the Match On FieldName
            MatchOnFieldName = fieldItem.Fields["Match On FieldName"].Value;
            if (String.IsNullOrEmpty(MatchOnFieldName))
            {
                throw new InvalidValueException(String.Format("The 'MatchOnFieldName' was not provided. Therefor it wasn't possible to match the importValue with a sourcelist. ItemId: {0}.", fieldItem.ID));
            }
        }

        #endregion Constructor

        #region Methods

        public override IEnumerable<Item> GetMatchingChildItem(BaseDataMap map, Item listParent, string importValue)
        {
            IEnumerable<Item> t = (from Item c in listParent.GetChildren()
                                   where c[MatchOnFieldName].ToLower().Equals(importValue.ToLower())
                                   select c).ToList();
            return t;
        }
        
        #endregion Methods
    }
}