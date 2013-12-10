using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.Mappings.FieldStorageHandlers;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.Providers;

namespace Sitecore.SharedSource.UserSync.Mappings {
	
    /// <summary>
    /// this is the class that all fields/properties should extend. 
    /// </summary>
    public class BaseMapping {

		#region Properties

		private string _newItemField;
        private const string FieldNameFieldStorageHandler = "Field Storage Handler";

		/// <summary>
		/// the field on the new item that the imported data should be stored in
		/// </summary>
        public string NewItemField {
			get {
				return _newItemField;
			}
			set {
				_newItemField = value;
			}
		}

        public Item FieldItem { get; set; }
        public BaseDataMap Map { get; set; }

        public bool IsRequiredOnImportRow { get; set; }
        public bool IsRequiredOnUser { get; set; }

		private string _HandlerClass;
		/// <summary>
		/// the class that represents the field
		/// </summary>
        public string HandlerClass {
			get {
				return _HandlerClass;
			}
			set {
			_HandlerClass = value;
			}
		}

		private string _HandlerAssembly;
		/// <summary>
		/// the assembly that the class representing this field is stored in
		/// </summary>
        public string HandlerAssembly {
			get {
				return _HandlerAssembly;
			}
			set {
				_HandlerAssembly = value;
			}
		}

        private BaseFieldStorageHandler fieldStorageHandler;
        /// <summary>
        /// the delimiter you want to separate imported data with
        /// </summary>
        public BaseFieldStorageHandler FieldStorageHandler
        {
            get
            {
                return fieldStorageHandler;
            }
            set
            {
                fieldStorageHandler = value;
            }
        }
        
		#endregion Properties

		#region Constructor

		public BaseMapping(BaseDataMap map, Item fieldItem)
		{
            Map = map; 
            FieldItem = fieldItem;
		    NewItemField = fieldItem.Fields["To What Field"].Value;

			HandlerClass = fieldItem.Fields["Handler Class"].Value;
			HandlerAssembly = fieldItem.Fields["Handler Assembly"].Value;
		    IsRequiredOnImportRow = fieldItem.Fields["Is Required On ImportRow"].Value == "1";
            IsRequiredOnUser = fieldItem.Fields["Is Required On User"].Value == "1";
        
            InitializeFieldStorageHandlerField(map, fieldItem);
		}

        #endregion Constructor

		#region Methods

        public virtual string ProcessImportedValue(string importValue, ref string errorMessage)
        {
            return importValue;
        }

        public virtual string FillField(BaseDataMap map, object importRow, ref User user, string importValue, out bool updatedField)
        {
            updatedField = false;
            if (IsRequiredOnImportRow)
            {
                if (String.IsNullOrEmpty(importValue))
                {
                    return String.Format("The imported value '{0}' was empty. This field must be provided when the field is marked as required on import row. " +
                                         "The field was not updated. User: {1}. ImportRow: {2}. FieldName: {3}.", importValue, map.GetUserDebugInfo(user), map.GetImportRowDebugInfo(importRow), NewItemField);
                }
            }
            if (FieldStorageHandler != null)
            {
                string errorMessage = String.Empty;
                var processedImportValue = ProcessImportedValue(importValue, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return String.Format("The processedImportValue '{0}' resulted in an error. The field was not updated. ErrorMessage: {1}. " +
                                         "User: {2}. ImportRow: {3}. FieldName: {4}. ImportValue: {5}.", processedImportValue, errorMessage, map.GetUserDebugInfo(user), map.GetImportRowDebugInfo(importRow), NewItemField, importValue);
                }
                if (IsRequiredOnUser)
                {
                    if (String.IsNullOrEmpty(processedImportValue))
                    {
                        return String.Format("The processedImportValue '{0}' was empty or null. This field cannot be empty or null when the field is marked as required on the user. " +
                                             "The field was not updated. User: {1}. ImportRow: {2}. FieldName: {3}. ImportValue: {4}.", processedImportValue, map.GetUserDebugInfo(user), map.GetImportRowDebugInfo(importRow), NewItemField, importValue);
                    }
                }
                var statusMessage = FieldStorageHandler.FillField(map, importRow, ref user, NewItemField, processedImportValue, IsRequiredOnUser, out updatedField);
                if (!String.IsNullOrEmpty(statusMessage))
                {
                    return String.Format("An error occured trying to fill the field with a value. The field was not updated. See error log: '{0}'.", statusMessage);
                }
            }
            else
            {
                return String.Format("The FillField failed because the FieldStorageHandler object was null. User: {0}. NewItemField: {1}. ImportValue: {2}.", map.GetUserDebugInfo(user), NewItemField, importValue);
            }
            return String.Empty;
        }

        private void InitializeFieldStorageHandlerField(BaseDataMap map, Item fieldItem)
        {
            var fieldStorageHandlerId = fieldItem.Fields[FieldNameFieldStorageHandler].Value;
            if (!String.IsNullOrEmpty(fieldStorageHandlerId))
            {
                if (Data.ID.IsID(fieldStorageHandlerId))
                {
                    Item fieldStorageHandlerItem = map.SitecoreDB.GetItem(fieldStorageHandlerId);
                    string errorMessage;
                    var storageHandler = CreateFieldStorageHandler(map, fieldStorageHandlerItem, out errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        map.LogBuilder.Log("Error",
                                       string.Format("The field '{0}' had a correct Sitecore ID, but the instantiation of the object failed. See the error log: {1}. " +
                                                     "FieldValue: {2}. The fieldItem: {3}.", FieldNameFieldStorageHandler, errorMessage, fieldStorageHandlerId, map.GetItemDebugInfo(fieldItem)));
                    }
                    if (storageHandler != null)
                    {
                        FieldStorageHandler = storageHandler;
                    }
                    else
                    {
                        map.LogBuilder.Log("Error",
                                       string.Format("The field '{0}' had a correct Sitecore ID, but the object was null." +
                                                     "FieldValue: {1}. The fieldItem: {2}.", FieldNameFieldStorageHandler, fieldStorageHandlerId, map.GetItemDebugInfo(fieldItem)));
                    }
                }
                else
                {
                    map.LogBuilder.Log("Error",
                                       string.Format("The field '{0}' had a value, but it was not a correct Sitecore ID. Please provide a correct Sitecore ID for the field to define which FieldStorageHandler should handle the saving of the field to user in the Sitecore Membership Database. " +
                                                     "FieldValue: {1}. The fieldItem: {2}.", FieldNameFieldStorageHandler, fieldStorageHandlerId, map.GetItemDebugInfo(fieldItem)));
                }
            }
            else
            {
                map.LogBuilder.Log("Error",
                                   string.Format("The field '{0}' was null or empty. Please provide a value for the field to define which FieldStorageHandler should handle the saving of the field to user in the Sitecore Membership Database. " +
                                                 "The fieldItem: {1}.", FieldNameFieldStorageHandler, map.GetItemDebugInfo(fieldItem)));
            }
        }

        private BaseFieldStorageHandler CreateFieldStorageHandler(BaseDataMap map, Item fieldStorageHandlerItem, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (fieldStorageHandlerItem!=null)
            {
                var handlerClass = fieldStorageHandlerItem.Fields["Handler Class"].Value;
                var handlerAssembly = fieldStorageHandlerItem.Fields["Handler Assembly"].Value;

                if (!string.IsNullOrEmpty(handlerClass))
                {
                    //create the object from the class and cast as IFieldStorageHandler
                    try
                    {
                        var storageHandler = (BaseFieldStorageHandler)Reflection.ReflectionUtil.CreateObject(handlerAssembly, handlerClass, new object[] { map.CheckThatCustomPropertyExistOnUserProfile });
                        if (storageHandler != null)
                        {
                            return storageHandler;
                        }
                        errorMessage += string.Format("The field: '{0}' class type '{1}' could not be instantiated",
                                            fieldStorageHandlerItem.Name, handlerClass);
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        errorMessage += string.Format("The field: {0} binary '{1}' specified could not be found. Exception: {1}", fieldStorageHandlerItem.Name, handlerAssembly, fnfe.Message);
                    }
                }
                else
                {
                    errorMessage += String.Format("the field: '{0}' Handler Class {1} is not defined", fieldStorageHandlerItem.Name,
                                      handlerClass);
                }
            }
            else
            {
                errorMessage += String.Format("The fieldStorageHandlerItem is null. Therefore the FieldStorageHandler could not be instantiated.");
            }
            return null;
        }

        #endregion Methods
	}
}
