using System;
using System.Collections.Generic;
using System.Globalization;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.Providers;
using System.Text;
using System.Text.RegularExpressions;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields
{
    /// <summary>
    /// this stores the plain text import value as is into the new field
    /// </summary>
    public class ToEmailField : ToTextField
    {
        private string EmailValidationRegex { get; set; }
        private bool SaveEmailAsLowerCase { get; set; }

        public ToEmailField(BaseDataMap map, Item fieldItem)
            : base(map, fieldItem)
        {
            InitializeSaveEmailAsLowerCase(map,fieldItem);
            InitializeEmailValidationRegex(map,fieldItem);
        }


        public override string FillField(BaseDataMap map, object importRow, ref Security.Accounts.User user, string importValue, out bool updatedField)
        {
            updatedField = false;
            if (!String.IsNullOrEmpty(importValue) || IsRequiredOnImportRow)
            {
                if (IsValidEmail(importValue))
                {
                    var statusMessage = base.FillField(map, importRow, ref user, importValue, out updatedField);
                    if (!String.IsNullOrEmpty(statusMessage))
                    {
                        return
                            String.Format(
                                "An error occured trying to fill the field with a value. The field was not updated. See error log: '{0}'.",
                                statusMessage);
                    }
                }
                else
                {
                    return String.Format("The value '{0}' is not a valid email. This is required since the 'Is Required On ImportRow' was true for this field. The field was not updated.", importValue);
                }
            }
            return String.Empty;
        }

        public override string ProcessImportedValue(string importValue, ref string errorMessage)
        {
            var importedValueAsLower = importValue.ToLower(CultureInfo.CurrentUICulture);
            if (SaveEmailAsLowerCase &&
                 importedValueAsLower != importValue)
            {
                return importedValueAsLower;
            }
            return importValue;
        }

        private void InitializeSaveEmailAsLowerCase(BaseDataMap map, Item i)
        {
            try
            {
                var saveEmailAsLowerCase = i.Fields["Save Email As Lower Case"].Value=="1";
                SaveEmailAsLowerCase = saveEmailAsLowerCase;
            }
            catch (Exception ex)
            {
                map.LogBuilder.Log("Error",String.Format("The value for email validation regex was not provided or the value could not be parsed. Exception:{0}",map.GetExceptionDebugInfo(ex)));
                EmailValidationRegex = @"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,4})$";
            }
        }
        private void InitializeEmailValidationRegex(BaseDataMap map, Item i)
        {
            try
            {
                var emailValidationRegex = i.Fields["Email Validation Regex"].Value;
                EmailValidationRegex = emailValidationRegex;
            }
            catch (Exception ex)
            {
                map.LogBuilder.Log("Error",String.Format("The value for email validation regex was not provided or the value could not be parsed. Exception:{0}",map.GetExceptionDebugInfo(ex)));
                EmailValidationRegex = @"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,4})$";
            }
        }

        private bool IsValidEmail(string importValue)
        {
            return Regex.IsMatch(importValue, EmailValidationRegex);
        }
    }
}
