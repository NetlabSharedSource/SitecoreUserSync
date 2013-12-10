using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.FieldStorageHandlers
{
    public class ProfilePropertyHandler : BaseFieldStorageHandler
    {
        public ProfilePropertyHandler(bool checkThatCustomPropertyExist)
            : base(checkThatCustomPropertyExist)
        {
        }

        public override string FillField(BaseDataMap map, object importRow, ref User user, string fieldName, string importValue, bool isRequiredOnUser,
                                      out bool updatedField)
        {
            updatedField = false;

            var profile = user.Profile;
            if (profile != null)
            {
                var propertyInfo = Sitecore.Reflection.ReflectionUtil.GetPropertyInfo(profile, fieldName);
                if (propertyInfo != null)
                {
                    if (propertyInfo.PropertyType == typeof (string) || propertyInfo.PropertyType == typeof (String))
                    {
                        var existingValue = propertyInfo.GetValue(profile, null) as string;
                        if (existingValue != importValue)
                        {
                            Sitecore.Reflection.ReflectionUtil.SetProperty(profile, propertyInfo, importValue);
                            updatedField = true;
                        }
                    }
                    else
                    {
                        return String.Format("The property on the User.Profile was not of type string. Therefore the field could not be filled. PropertyType: {0}. User '{1}'. FieldName: {2}. ImportValue: {3}.", propertyInfo.PropertyType, map.GetUserDebugInfo(user), fieldName, importValue);
                    }
                }
                else
                {
                    if (CheckThatPropertyExist)
                    {
                        return
                            String.Format(
                                "The profile did not contain a property as defined in fieldName. This field must be found on the user because the 'isRequired' value is set. User '{0}'. FieldName: {1}. ImportValue: {2}.",
                                map.GetUserDebugInfo(user), fieldName, importValue);
                    }
                }
            }
            else
            {
                return String.Format("The profile was null on the user '{0}' while processing the fieldName: {1}, with importValue: {2}.", map.GetUserDebugInfo(user), fieldName, importValue);
            }
            return string.Empty;
        }
    }
}