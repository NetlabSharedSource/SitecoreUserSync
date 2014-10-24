using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Providers;
using System;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields
{
    public class ToRoleMembership : ToTextField
    {
        protected String RoleName { get; set; }
        protected String TrueValue { get; set; }

        public ToRoleMembership(BaseDataMap map, Item fieldItem)
            : base(map, fieldItem)
        {
            RoleName = fieldItem["To What Field"];
            TrueValue = fieldItem["True Value"];
        }



        public override string FillField(BaseDataMap map, object importRow,
                                         ref User user,
                                         string importValue,
                                         out bool updatedField)
        {
            updatedField = false;
            var errorMessage = String.Empty;
            var shouldBeMemberOfRole = !String.IsNullOrEmpty(importValue) &&
                                       importValue.Trim() == TrueValue;
            var role = InitializeRoleFromString(RoleName, ref errorMessage);
            if (role != null)
            {
                if (!user.IsInRole(role) && shouldBeMemberOfRole)
                {
                    user.Roles.Add(role);
                    updatedField = true;
                }
                else if (user.IsInRole(role) && !shouldBeMemberOfRole)
                {
                    user.Roles.Remove(role);
                    updatedField = true;

                }
            }
            return errorMessage;
        }

        private Role InitializeRoleFromString(string roleName, ref string errorMessage)
        {
            if (Role.Exists(roleName))
            {
                var role = Role.FromName(roleName);
                if (role != null)
                {
                    return role;
                }
                errorMessage += "Could not initiate role " + roleName;
            }
            errorMessage += "Role " + roleName + " doesn't exist";
            return null;
        }
    }
}