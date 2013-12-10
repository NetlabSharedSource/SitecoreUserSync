using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.UserKeyHandlers
{
    public class UserNameAsKeyHandler : BaseUserKeyHandler
    {
        public UserNameAsKeyHandler(BaseDataMap map)
            : base(map)
        {
        }
        
        public override string GetKeyValueFromUser(User user, ref string errorMessage)
        {
            try
            {
                if (user != null)
                {
                    return user.LocalName;
                }
                errorMessage += String.Format(
                    "The user was null in the GetKeyValueFromUser method, when trying to get the KeyValue from the User.");
            }
            catch (Exception ex)
            {
                errorMessage +=
                    String.Format(
                        "The GetKeyValueFromUser thrown an exception in trying to get the KeyValue from the User. User: {0}. Exception: {1}",
                        Map.GetUserDebugInfo(user), ex);
            }
            return null;
        }

        public override List<User> GetUsersByKeyValue(string keyValue, ref string errorMessage)
        {
            try
            {
                var fullName = Map.CreateUserInWhatSecurityDomain.GetFullName(keyValue);
                if (User.Exists(fullName))
                {
                    User user = User.FromName(fullName, true);
                    if (user != null)
                    {
                        return new List<User>() {user};
                    }
                    errorMessage +=
                        String.Format(
                            "The GetUserByKey tried to get the user, but it was null. FullName: {0}. KeyValue: {1}.",
                            fullName, keyValue);
                    return null;
                }
            }
            catch (Exception ex)
            {
                errorMessage +=
                    String.Format(
                        "The GetUserByKey thrown an exception in trying to query the item. Exception: {0}", ex);
                return null;
            }
            return new List<User>();
        }
    }
}