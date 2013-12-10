using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage
{
    public class MobileAliasUserKeyStorage: BaseUserKeyStorage
    {
        private const string CommandPatternGetUserName = "SELECT [UserName] FROM aspnet_Users WHERE ([{0}]='{1}')";
        private const string CommandPatternGetMobileAlias = "SELECT [{0}] FROM aspnet_Users WHERE ([UserName]='{1}')";
        private const string CommandPatternUpdateMobileAlias = "UPDATE aspnet_Users SET {0} = '{1}' WHERE (UserName = '{2}')";


        protected override string GetCommandPatternGetUserNameFromKey()
        {
            return CommandPatternGetUserName;
        }

        protected override string GetCommandPatternGetKey()
        {
            return CommandPatternGetMobileAlias;
        }

        protected override string GetCommandPatternUpdateKey()
        {
            return CommandPatternUpdateMobileAlias;
        }
    }
}