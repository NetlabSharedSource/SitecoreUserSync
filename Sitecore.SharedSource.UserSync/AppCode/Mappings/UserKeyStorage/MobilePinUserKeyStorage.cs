using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage
{
    public class MobilePinUserKeyStorage: BaseUserKeyStorage
    {
        private const string CommandPatternGetUserName = "SELECT [UserName] FROM aspnet_Users INNER JOIN aspnet_Membership ON aspnet_Users.UserId = aspnet_Membership.UserId " +
                                                         "WHERE ([{0}]='{1}')";
        private const string CommandPatternGetMobilePin = "SELECT [{0}] FROM aspnet_Users INNER JOIN aspnet_Membership ON aspnet_Users.UserId = aspnet_Membership.UserId " +
                                                            "WHERE ([UserName]='{1}')";
        private const string CommandPAtternUpdateMobilePin = "UPDATE aspnet_Membership SET {0} = '{1}' WHERE (UserId IN " +
            "(SELECT aspnet_Users.UserId FROM aspnet_Users INNER JOIN aspnet_Membership AS aspnet_Membership_1 ON aspnet_Users.UserId = aspnet_Membership_1.UserId " +
            "WHERE (aspnet_Users.UserName = '{2}')))";
        
        protected override string GetCommandPatternGetUserNameFromKey()
        {
            return CommandPatternGetUserName;
        }

        protected override string GetCommandPatternGetKey()
        {
            return CommandPatternGetMobilePin;
        }

        protected override string GetCommandPatternUpdateKey()
        {
            return CommandPAtternUpdateMobilePin;
        }
    }
}