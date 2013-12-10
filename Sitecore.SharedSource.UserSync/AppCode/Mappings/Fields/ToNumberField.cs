using System;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.Mappings.Fields;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.AppCode.Mappings.Fields
{
    public class ToNumberField: ToTextField
    {
        private int MustBeEqualToOrHigherThan = Int32.MinValue;
        private int MustBeEqualToOrLowerThan = Int32.MaxValue;

        public ToNumberField(BaseDataMap map, Item fieldItem) : base(map, fieldItem)
        {
            InitializeMinimumAndMaximumValues(map,fieldItem);
        }
        public override string FillField(BaseDataMap map, object importRow, ref Security.Accounts.User user, string importValue, out bool updatedField)
        {
            updatedField = false;
            if (!String.IsNullOrEmpty(importValue) || IsRequiredOnImportRow)
            {
                if (IsValidNumber(importValue, map))
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
                    return String.Format("The value '{0}' is not a valid email. The field was not updated.", importValue);
                }
            }
            return String.Empty;
        }

        private bool IsValidNumber(string importValue, BaseDataMap map)
        {
            int importValueAsInteger;
            if (!int.TryParse(importValue, out importValueAsInteger))
            {
                map.LogBuilder.Log("Error", String.Format("The value '{0}' could not be parsed as an integer.", importValue));
                return false;
            }
            if (importValueAsInteger >= MustBeEqualToOrHigherThan &&
                importValueAsInteger <= MustBeEqualToOrLowerThan)
            {
                return true;    
            }
            map.LogBuilder.Log("Error", String.Format("The value '{0}' was either higher than or lower than the allowed range min:'{1}' max:{2} .", importValue, MustBeEqualToOrHigherThan, MustBeEqualToOrHigherThan));
            return false;
        }

        private void InitializeMinimumAndMaximumValues(BaseDataMap map, Item i)
        {
            const string mustBeEqualToOrHigherThanFieldName = "Must Be Equal To or Higher Than";
            const string mustBeEqualToOrLowerThanFieldName = "Must Be Equal To or Lower Than";
            if (!string.IsNullOrEmpty(i[mustBeEqualToOrHigherThanFieldName]))
            {
                int mustBeEqualToOrHigherThan;
                if (int.TryParse(i[mustBeEqualToOrHigherThanFieldName], out mustBeEqualToOrHigherThan))
                {
                    MustBeEqualToOrHigherThan = mustBeEqualToOrHigherThan;
                }
                else
                {
                    map.LogBuilder.Log("Error", String.Format("The value for 'Must Be Equal To or Higher Than' could not be parsed. value:{0}", mustBeEqualToOrHigherThan));
                }

            }
            if (!string.IsNullOrEmpty(i[mustBeEqualToOrLowerThanFieldName]))
            {
                int mustBeEqualToOrLowerThan;
                if (int.TryParse(i[mustBeEqualToOrLowerThanFieldName], out mustBeEqualToOrLowerThan))
                {
                    MustBeEqualToOrLowerThan = mustBeEqualToOrLowerThan;
                }
                else
                {
                    map.LogBuilder.Log("Error", String.Format("The value for 'Must Be Equal To or Lower Than' could not be parsed. value:{0}", mustBeEqualToOrLowerThan));
                }

            }
        }
    }
}