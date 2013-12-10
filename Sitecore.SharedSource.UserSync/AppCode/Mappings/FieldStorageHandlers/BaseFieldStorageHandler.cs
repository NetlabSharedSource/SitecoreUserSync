using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.FieldStorageHandlers
{
    public abstract class BaseFieldStorageHandler
    {
        public BaseUserKeyStorage UserKeyStorage { get; set; }

        public bool CheckThatPropertyExist { get; set;}

        public BaseFieldStorageHandler()
        {
            CheckThatPropertyExist = false;
        }

        public BaseFieldStorageHandler(bool checkThatCustomPropertyExist)
        {
            CheckThatPropertyExist = checkThatCustomPropertyExist;
        }

        public abstract string FillField(BaseDataMap map, object importRow, ref User user, string fieldName,
                                      string importValue, bool isRequired,
                                      out bool updatedField);
    }
}