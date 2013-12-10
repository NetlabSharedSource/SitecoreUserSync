using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.FieldStorageHandlers
{
    public class ProfileCustomPropertyHandler : BaseFieldStorageHandler
    {
        public ProfileCustomPropertyHandler(bool checkThatCustomPropertyExist)
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
                if (CheckThatPropertyExist)
                {
                    var customPropertiesNames = profile.GetCustomPropertyNames();
                    if (customPropertiesNames != null)
                    {
                        if (!customPropertiesNames.Contains(fieldName))
                        {
                            return String.Format(
                                    "The profile does not contain a Custom Property as defined in fieldName. " + 
                                    "User '{0}'. FieldName: {1}. ImportValue: {2}.",
                                    map.GetUserDebugInfo(user), fieldName, importValue);
                        }
                    }
                    else
                    {
                        return String.Format("The profile.GetCustomPropertyNames() returned null. Therefore the custom property could not be verified if it exists. User '{0}'. FieldName: {1}. ImportValue: {2}.", map.GetUserDebugInfo(user), fieldName, importValue);
                    }
                }
                var existingValue = profile.GetCustomProperty(fieldName);
                if (existingValue != importValue)
                {
                    profile.SetCustomProperty(fieldName, importValue);
                    updatedField = true;
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