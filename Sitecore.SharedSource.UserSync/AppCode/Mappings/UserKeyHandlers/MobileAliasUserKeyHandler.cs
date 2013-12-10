using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage;
using Sitecore.SharedSource.UserSync.AppCode.Utility;
using Sitecore.SharedSource.UserSync.Mappings.Fields;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.UserKeyHandlers
{
    public class MobileAliasUserKeyHandler : BaseUserKeyHandler
    {
        public MobileAliasUserKeyHandler(BaseDataMap map) : base(map)
        {
            UserKeyStorage = new MobileAliasUserKeyStorage();
        }

        public MobileAliasUserKeyHandler(BaseDataMap map, IBaseField fieldDefinition)
            : base(map, fieldDefinition)
        {
            UserKeyStorage = new MobileAliasUserKeyStorage();
        }

        public override string GetKeyValueFromUser(User user, ref string errorMessage)
        {
            try
            {
                if (user != null)
                {
                    var keyValue = UserKeyStorage.GetKeyValueFromUser(Map.CreateUserInWhatSecurityDomain.GetFullName(user.LocalName), FieldDefinition.GetNewItemField(), ref errorMessage);
                    return keyValue;
                }
                errorMessage += String.Format(
                        "The user was null in the GetKeyValueFromUser method, when trying to get the KeyValue from the User.");
            }
            catch (Exception ex)
            {
                errorMessage +=
                    String.Format(
                        "The GetKeyValueFromUser thrown an exception in trying to get the KeyValue from the User. ToWhatField: {0}. User: {1}. Exception: {2}", FieldDefinition.GetNewItemField(), Map.GetUserDebugInfo(user), ex);
            }
            return null;
        }

        public override List<User> GetUsersByKeyValue(string keyValue, ref string errorMessage)
        {
            try
            {
                List<User> list = UserKeyStorage.GetUsersFromKey(FieldDefinition.GetNewItemField(), keyValue, ref errorMessage);
                return list;
            }
            catch (Exception ex)
            {
                errorMessage +=
                    String.Format(
                        "The GetUserByKey thrown an exception in trying to query the item. ToWhatField: {0}. Exception: {1}", FieldDefinition.GetNewItemField(), ex);
                return null;
            }
        }
    }
}