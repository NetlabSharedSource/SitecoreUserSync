using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage;
using Sitecore.SharedSource.UserSync.Mappings.Fields;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.UserKeyHandlers
{
    public abstract class BaseUserKeyHandler
    {
        public BaseUserKeyStorage UserKeyStorage { get; set; }
        public IBaseField FieldDefinition { get; set; }

        protected BaseDataMap Map { get; private set; }

        protected BaseUserKeyHandler(BaseDataMap map)
        {
            Map = map;
        }

        protected BaseUserKeyHandler(BaseDataMap map, IBaseField fieldDefinition)
        {
            Map = map;
            FieldDefinition = fieldDefinition;
        }

        public virtual string GetKeyValueFromImportRow(object importRow, ref string errorMessage)
        {
            if (Map.IdentifyUserUniqueByFieldDefinition != null)
            {
                var existingFieldNames = FieldDefinition.GetExistingFieldNames();
                var fieldValueDelimiter = FieldDefinition.GetFieldValueDelimiter();
                IEnumerable<string> values = Map.GetFieldValues(existingFieldNames, importRow, ref errorMessage);
                return String.Join(fieldValueDelimiter, values.ToArray());
            }
            return Map.GetFieldValue(importRow, Map.GetUsernameFromWhatField, ref errorMessage);
        }

        public abstract string GetKeyValueFromUser(User user, ref string errorMessage);

        public abstract List<User> GetUsersByKeyValue(string keyValue, ref string errorMessage);
    }
}