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
    public class ToGuidFromListValueMatchOnDisplayNameField : ToTextField
    {
        #region Properties

        private bool _DoNotRequireValueMatch;

        public bool DoNotRequireValueMatch
        {
            get
            {
                return _DoNotRequireValueMatch;
            }
            set
            {
                _DoNotRequireValueMatch = value;
            }
        }

        private string _SourceList;
        /// <summary>
        /// This is the list that you will compare the imported values against
        /// </summary>
        public string SourceList
        {
            get
            {
                return _SourceList;
            }
            set
            {
                _SourceList = value;
            }
        }

        #endregion Properties

        #region Constructor

        public ToGuidFromListValueMatchOnDisplayNameField(BaseDataMap map, Item fieldItem): base(map, fieldItem)
        {
            //stores the source list value
            SourceList = fieldItem.Fields["Source List"].Value;
            if (String.IsNullOrEmpty(SourceList))
            {
                throw new InvalidValueException(String.Format("The 'Source List' was not provided. Therefore it wasn't possible to match the importValue with a sourcelist. ItemId: {0}.", fieldItem.ID));
            }
            DoNotRequireValueMatch = fieldItem.Fields["Do Not Require Value Match"].Value == "1";
        }

        #endregion Constructor

        #region Methods

        public virtual IEnumerable<Item> GetMatchingChildItem(BaseDataMap map, Item listParent, string importValue)
        {
            IEnumerable<Item> t = (from Item c in listParent.GetChildren()
                                   where c.DisplayName.ToLower().Equals(StringUtility.GetNewItemName(importValue, 60))
                                   select c).ToList();
            return t;
        }

        public override string ProcessImportedValue(string importValue, ref string errorMessage)
        {
            if (!ID.IsID(SourceList))
            {
                errorMessage = String.Format(
                        "The 'Source List' provided was not an valid Sitecore ID. SourceList: {0}. The Fill Field method was aborted and the fieldvalue was not updated.",
                        SourceList);
            }
            //get parent item of list to search
            var sourceListItem = FieldItem.Database.GetItem(SourceList);
            if (sourceListItem != null)
            {
                if (!String.IsNullOrEmpty(importValue))
                {
                    //loop through children and look for anything that matches by name
                    var matchingListItems = GetMatchingChildItem(Map, sourceListItem, importValue);
                    var listItems = matchingListItems.ToList();
                    //if you find one then store the id
                    if (listItems.Count() > 1)
                    {
                        errorMessage = String.Format(
                            "An attempt to lookup the ListValue resultet in more than one item and therefore it failed. " +
                            "The field '{0}', but the imported value '{1}' did result in more that one lookup item. The field was not updated. Count: {2}.",
                            NewItemField, importValue, listItems.Count);
                    }
                    if (listItems.Count() == 1)
                    {
                        var guid = listItems.First().ID.ToString();
                        if (ID.IsID(guid))
                        {
                            return guid;
                        }
                        errorMessage = String.Format(
                            "An attempt to lookup the ListValue resultet in one item, but the ID didn't result in a valid ID." +
                            "Field '{0}'. The imported value '{1}'. The field was not updated.",
                            NewItemField, importValue);
                    }
                    else
                    {
                        if (!DoNotRequireValueMatch)
                        {
                            errorMessage = String.Format(
                                "An attempt to lookup the ListValue resultet in no items and since the field is marked as RequiredValueMatch = true, it failed. " +
                                "The field '{0}'. The imported value '{1}'. The field was not updated. Count: {2}.",
                                NewItemField, importValue, listItems.Count);
                        }
                    }
                }
                else
                {
                    if (IsRequiredOnImportRow)
                    {
                        errorMessage =
                            String.Format(
                                "An attempt to lookup the ListValue resultet in no items and since the field is marked as RequiredValueMatch = true, it failed. " +
                                "The field '{0}'. The imported value '{1}'. The field was not updated.",
                                NewItemField, importValue);
                    }
                }
            }
            return String.Empty;
        }
        #endregion Methods
    }
}