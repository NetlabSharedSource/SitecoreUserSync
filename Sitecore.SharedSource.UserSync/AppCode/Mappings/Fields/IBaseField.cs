using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using System.Data;
using System.Collections;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Mappings.FieldStorageHandlers;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings.Fields {
    /// <summary>
    /// The IBaseField is the interface required for all Data Importer fields to function properly
    /// </summary>
    public interface IBaseField
    {
        #region Methods

        string GetNewItemField();

        /// <summary>
        /// This uses the imported value to modify the newly created item. 
        /// </summary>
        /// <param name="map">provides settings related to the import</param>
        /// <param name="user">the newly created item</param>
        /// <param name="importValue">the imported value</param>
        /// <param name="updatedField">indicates if the field has been updated</param>
        string FillField(BaseDataMap map, object importRow, ref User user, string importValue, out bool updatedField);

        string ProcessImportedValue(string importValue, ref string errorMessage);

        /// <summary>
        /// returns a list of the field names from the import row that you want to import into this field 
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetExistingFieldNames();

        /// <summary>
        /// returns a delimiter to use between the fields being imported
        /// </summary>
        /// <returns></returns>
        string GetFieldValueDelimiter();

        BaseFieldStorageHandler FieldStorageHandler { get; set; }

        #endregion Methods
    }
}
