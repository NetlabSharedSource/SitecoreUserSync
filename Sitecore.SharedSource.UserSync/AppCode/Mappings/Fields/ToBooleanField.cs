using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.Mappings.Fields;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields
{
    public class ToBooleanField: ToTextField
    {
        private const string FieldNameWhatStringToIdentifyTrueBoolValue = "What String To Identify True Bool Value";
        private const string FieldNameWhatStringToIdentifyFalseBoolValue = "What String To Identify False Bool Value";
        private string WhatStringToIdentifyTrueBoolValue { get; set; }
        private string WhatStringToIdentifyFalseBoolValue { get; set; }

        public ToBooleanField(BaseDataMap map, Item fieldItem)
            : base(map, fieldItem)
        {
            WhatStringToIdentifyTrueBoolValue = fieldItem.Fields[FieldNameWhatStringToIdentifyTrueBoolValue].Value;
            if (String.IsNullOrEmpty(WhatStringToIdentifyTrueBoolValue))
            {
                map.LogBuilder.Log("Error",
                                      string.Format("The field '{0}' didn't contain any value. A string value must be provided to identify the bool value. " +
                                                    "FieldValue: {1}. The fieldItem: {2}.", FieldNameWhatStringToIdentifyTrueBoolValue, WhatStringToIdentifyTrueBoolValue, map.GetItemDebugInfo(fieldItem)));
                    
            }
            WhatStringToIdentifyFalseBoolValue = fieldItem.Fields[FieldNameWhatStringToIdentifyFalseBoolValue].Value;
            if (String.IsNullOrEmpty(WhatStringToIdentifyFalseBoolValue))
            {
                map.LogBuilder.Log("Error",
                                      string.Format("The field '{0}' didn't contain any value. A string value must be provided to identify the bool value. " +
                                                    "FieldValue: {1}. The fieldItem: {2}.", FieldNameWhatStringToIdentifyFalseBoolValue, WhatStringToIdentifyFalseBoolValue, map.GetItemDebugInfo(fieldItem)));

            }
        }

        public override string ProcessImportedValue(string importValue, ref string errorMessage)
        {
            if (!String.IsNullOrEmpty(importValue))
            {
                var importedValueAsLower = importValue.ToLower(CultureInfo.CurrentUICulture);
                if (importedValueAsLower.Contains(WhatStringToIdentifyTrueBoolValue))
                {
                    return "1";
                }
                if (importedValueAsLower.Contains(WhatStringToIdentifyFalseBoolValue))
                {
                    return "0";
                }
            }
            return String.Empty;
        }
    }
}