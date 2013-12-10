using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.FieldStorageHandlers
{
    public class DbMembershipTableColumnHandler : BaseFieldStorageHandler
    {
        public DbMembershipTableColumnHandler()
        {
            UserKeyStorage = new MobilePinUserKeyStorage();
        }

        public DbMembershipTableColumnHandler(bool checkThatCustomPropertyExist)
            : base(checkThatCustomPropertyExist)
        {
            UserKeyStorage = new MobilePinUserKeyStorage();
        }

        public override string FillField(BaseDataMap map, object importRow, ref User user, string fieldName, string importValue, bool isRequiredOnUser,
                                      out bool updatedField)
        {
            updatedField = false;

            string errorMessage = String.Empty;
            var users = UserKeyStorage.GetUsersFromKey(fieldName, importValue, ref errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                return String.Format(
                                    "An error occured in the GetUsersFromKey method in the FillField method in trying to retrieve the users from key. ErrorMessage: {1}. " +
                                    "User '{2}'. FieldName: {0}. ImportValue: {3}.",
                                    fieldName, errorMessage, map.GetUserDebugInfo(user), importValue);
            }
            if (users.Count > 1)
            {
                return String.Format(
                                    "Duplicate values where found in the column '{0}' in the Users table in the .NET membership database. The value must be unique. ErrorMessage: {1}. " +
                                    "User '{2}'. FieldName: {0}. ImportValue: {3}.",
                                    fieldName, errorMessage, map.GetUserDebugInfo(user), importValue);
            }
            if (users.Count == 1)
            {
                if (users.First().LocalName != user.LocalName)
                {
                    return String.Format(
                                    "A key with the same value in column '{0}' was found on another user in the Users table in the .NET membership database. The value must be unique. " +
                                    "ErrorMessage: {1}. Found on user: {2}. Current user '{3}'. FieldName: {0}. ImportValue: {4}.",
                                    fieldName, errorMessage, map.GetUserDebugInfo(users.First()), map.GetUserDebugInfo(user), importValue);
                }
            }

            var result = UserKeyStorage.UpdateKeyValueToDotnetMemberShipProviderDatabase(fieldName, importValue, user.Domain.GetFullName(user.LocalName), ref errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                return String.Format(
                                    "The update of column '{0}' in the Membership table in the .NET membership database failed of the following reason: {1}. " +
                                    "User '{2}'. FieldName: {0}. ImportValue: {3}.",
                                    fieldName, errorMessage, map.GetUserDebugInfo(user), importValue);
            }
            updatedField = result == 1;
            return string.Empty;
        }
    }
}