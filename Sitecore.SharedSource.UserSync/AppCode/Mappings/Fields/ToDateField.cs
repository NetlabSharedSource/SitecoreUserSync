using System;
using System.Globalization;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields
{
    public class ToDateField: ToTextField
    {
        private const string FieldNameFromWhatDateTimeFormat = "From What DateTime Format";
        private const string FieldNameToWhatDateTimeFormat = "To What DateTime Format";

        public string FromWhatDateTimeFormat { get; set; }
        public string ToWhatDateTimeFormat { get; set; }

        public ToDateField(BaseDataMap map, Item fieldItem) : base(map, fieldItem)
        {
            FromWhatDateTimeFormat = fieldItem.Fields[FieldNameFromWhatDateTimeFormat].Value;
            if (String.IsNullOrEmpty(FromWhatDateTimeFormat))
            {
                map.LogBuilder.Log("Error",
                                      String.Format("The field '{0}' didn't contain any value. A string value must be provided for the DateTime format to parse the given date from. " +
                                                    "FieldValue: {1}. The fieldItem: {2}.", FieldNameFromWhatDateTimeFormat, FromWhatDateTimeFormat, map.GetItemDebugInfo(fieldItem)));
            }
            ToWhatDateTimeFormat = fieldItem.Fields[FieldNameToWhatDateTimeFormat].Value;
            if (String.IsNullOrEmpty(ToWhatDateTimeFormat))
            {
                map.LogBuilder.Log("Error",
                                      String.Format("The field '{0}' didn't contain any value. A string value must be provided for the DateTime format to parse the given date from. " +
                                                    "FieldValue: {1}. The fieldItem: {2}.", FieldNameToWhatDateTimeFormat, ToWhatDateTimeFormat, map.GetItemDebugInfo(fieldItem)));
            }
        }

        public override string ProcessImportedValue(string importValue, ref string errorMessage)
        {
            if (!String.IsNullOrEmpty(importValue))
            {
                try
                {
                    DateTime date = DateTime.ParseExact(importValue, FromWhatDateTimeFormat,
                                                        CultureInfo.InvariantCulture);
                    string dateString = date.ToString(ToWhatDateTimeFormat);
                    return dateString;
                }
                catch (Exception ex)
                {
                    errorMessage += String.Format(
                            "An error occured when trying to Parse the importValue as a DateTime or when trying to output the datetime to a string. Therefor the field was not updated. " +
                            "The importValue was '{0}'. FromWhatDateTimeFormat: {1}. ToWhatDateTimeFormat: {2}. The fieldName: {2}. Exception: {3}.",
                            importValue, FromWhatDateTimeFormat, ToWhatDateTimeFormat, NewItemField, Map.GetExceptionDebugInfo(ex));
                }
            }
            return String.Empty;
        }
    }
}