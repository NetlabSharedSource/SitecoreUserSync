using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.Security.Accounts;
using Sitecore.Security.Authentication;
using Sitecore.Security.Domains;
using Sitecore.SharedSource.UserSync.AppCode.Log;
using Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage;
using Sitecore.SharedSource.UserSync.Mappings.Fields;
using Sitecore.SharedSource.UserSync.Extensions;
using Sitecore.SharedSource.UserSync.Mappings;
using Sitecore.Collections;
using System.IO;
using Sitecore.SecurityModel;
using Sitecore.SharedSource.UserSync.Mappings.UserKeyHandlers;

namespace Sitecore.SharedSource.UserSync.Providers
{
    /// <summary>
    /// The BaseDataMap is the base class for any data provider. It manages values stored in sitecore 
    /// and does the bulk of the work processing the fields
    /// </summary>
    public abstract class BaseDataMap
    {
        private const int DefaultMinimumNumberOfRowsRequiredToStartImport = 10;
        private const int DefaultNumberOfRowsToProcessBeforeLogStatus = 10;

        private const string FieldAddUserToWhatStandardRoles = "Add User To What Standard Roles";
        private const string FieldOnPresentInImportAddToRoles = "On Present In Import Add To Roles";
        private const string FieldOnPresentInImportRemoveFromRoles = "On Present In Import Remove From Roles";
        private const string FieldOnNotPresentInImportAddToRoles = "On Not Present In Import Add To Roles";
        private const string FieldOnNotPresentInImportRemoveFromRoles = "On Not Present In Import Remove From Roles";

        private const string FieldCreateUserInWhatSecurityDomain = "Create User In What Security Domain";

        private const string DefaultUseWhatProfileItemId = "{AE4C4969-5B7E-4B4E-9042-B2D8701CE214}";

        #region Properties

        /// <summary>
        /// the log is returned with any messages indicating the status of the import
        /// </summary>
        protected Logging logBuilder;

        public Logging LogBuilder
        {
            get { return logBuilder; }
            set { logBuilder = value; }
        }

        private Database _SitecoreDB;

        /// <summary>
        /// the reference to the sitecore database that you'll import into and query from
        /// </summary>
        public Database SitecoreDB
        {
            get { return _SitecoreDB; }
            set { _SitecoreDB = value; }
        }

        private int _MinimumNumberOfRowsRequiredToRunTheImport;

        /// <summary>
        /// The minimum number of rows required to run the import
        /// </summary>
        public int MinimumNumberOfRowsRequiredToRunTheImport
        {
            get { return _MinimumNumberOfRowsRequiredToRunTheImport; }
            set { _MinimumNumberOfRowsRequiredToRunTheImport = value; }
        }

        public int LimitNumberOfImportRowsToWhatNumber { get; set; }

        public int StartOnWhatImportRowIndex { get; set; }

        private bool isValidateImportDataBeforeProcessing;

        public bool IsValidateImportDataBeforeProcessing
        {
            get { return isValidateImportDataBeforeProcessing; }
            set { isValidateImportDataBeforeProcessing = value; }
        }

        private bool isInValidationAllowDuplicatesInKey;

        public bool IsInValidationAllowDuplicatesInKey
        {
            get { return isInValidationAllowDuplicatesInKey; }
            set { isInValidationAllowDuplicatesInKey = value; }
        }

        /// <summary>
        /// the sitecore field value of fields used to build the new item name
        /// </summary>
        public string GetUsernameFromWhatField { get; set; }

        public Domain CreateUserInWhatSecurityDomain { get; set; }
        public List<Role> AddUserToWhatStandardRoles { get; set; }
        public int AutogeneratePasswordWithLength { get; set; }
        public string GetPasswordFromWhatField { get; set; }
        public List<Role> OnPresentInImportAddToRoles { get; set; }
        public List<Role> OnPresentInImportRemoveFromRoles { get; set; }

        public bool IsProcessUsersNotPresentInImport { get; set; }
        public List<Role> OnNotPresentInImportAddToRoles { get; set; }
        public List<Role> OnNotPresentInImportRemoveFromRoles { get; set; }

        public bool SetProfileItemOnUser { get; set; }
        public string UseWhatProfileItemId { get; set; }
        public bool CheckThatCustomPropertyExistOnUserProfile { get; set; }

        private List<IBaseField> _fieldDefinitions = new List<IBaseField>();

        /// <summary>
        /// the definitions of fields to import
        /// </summary>
        public List<IBaseField> FieldDefinitions
        {
            get { return _fieldDefinitions; }
            set { _fieldDefinitions = value; }
        }

        /// <summary>
        /// The field item used to identify equal items - the key field
        /// </summary>
        public IBaseField IdentifyUserUniqueByFieldDefinition { get; set; }

        private string _dataSourceString;

        /// <summary>
        /// the data source string fx connection string, or other datasource string
        /// </summary>
        public string DataSourceString
        {
            get { return _dataSourceString; }
            set { _dataSourceString = value; }
        }

        private string _Data;

        /// <summary>
        /// the query used to retrieve the data
        /// </summary>
        public string Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        private string _Query;

        /// <summary>
        /// the query used to retrieve the data
        /// </summary>
        public string Query
        {
            get { return _Query; }
            set { _Query = value; }
        }

        private bool _IsDoNotLogProgressStatusMessagesInSitecoreLog;

        public bool IsDoNotLogProgressStatusMessagesInSitecoreLog
        {
            get { return _IsDoNotLogProgressStatusMessagesInSitecoreLog; }
            set { _IsDoNotLogProgressStatusMessagesInSitecoreLog = value; }
        }

        public bool IsDeleteUsersWithMembershipInStandardRolesOnly { get; set; }

        public Item ImportItem { get; set; }

        protected BaseUserKeyHandler UserKeyHandler { get; set; }

        #endregion Properties

        #region Constructor

        protected BaseDataMap(Database db, Item importItem, Logging logging)
        {
            ImportItem = importItem;
            LogBuilder = logging;

            //setup import details
            SitecoreDB = db;
            // Defines the data and datasource for the import
            DataSourceString = importItem["Data Source"];
            Query = importItem["Query"];

            Int32.TryParse(importItem["Minimum Number Of Rows Required To Run The Import"],
                           out _MinimumNumberOfRowsRequiredToRunTheImport);
            if (MinimumNumberOfRowsRequiredToRunTheImport < 0)
            {
                MinimumNumberOfRowsRequiredToRunTheImport = 0;
            }
            IsValidateImportDataBeforeProcessing = importItem["Validate Import Data Before Processing"] == "1";
            IsInValidationAllowDuplicatesInKey = importItem["In Validation Allow Duplicates In Key"] == "1";

            int limitNumberOfImportRowsToWhatNumber;
            if (Int32.TryParse(importItem["Limit Number Of ImportRows To What Number"],
                               out limitNumberOfImportRowsToWhatNumber))
            {
                LimitNumberOfImportRowsToWhatNumber = limitNumberOfImportRowsToWhatNumber;
            }
            else
            {
                LimitNumberOfImportRowsToWhatNumber = 0;
            }
            if (LimitNumberOfImportRowsToWhatNumber < 0)
            {
                LimitNumberOfImportRowsToWhatNumber = 0;
            }

            int startOnWhatImportRowIndex;
            if (Int32.TryParse(importItem["Start On What ImportRow Index"],
                               out startOnWhatImportRowIndex))
            {
                StartOnWhatImportRowIndex = startOnWhatImportRowIndex;
            }
            else
            {
                StartOnWhatImportRowIndex = 0;
            }
            if (StartOnWhatImportRowIndex < 0)
            {
                StartOnWhatImportRowIndex = 0;
            }


            //more properties
            GetUsernameFromWhatField = importItem.Fields["Get Username From What Field"].Value;
            if (String.IsNullOrEmpty(GetUsernameFromWhatField))
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "The field '{0}' must contain a value that indicates which field to retrieve the username from.",
                                   "Get Username From What Field"));
            }

            InitializeIdentifyUserUniqueByFieldDefinition(importItem);

            InitializeUserKeyHandler();

            var createUserInWhatSecurityDomainString = importItem.Fields[FieldCreateUserInWhatSecurityDomain].Value;
            if (!String.IsNullOrEmpty(createUserInWhatSecurityDomainString))
            {
                if (DomainManager.DomainExists(createUserInWhatSecurityDomainString))
                {
                    var createUserInWhatSecurityDomain = DomainManager.GetDomain(createUserInWhatSecurityDomainString);
                    if (createUserInWhatSecurityDomain != null)
                    {
                        CreateUserInWhatSecurityDomain = createUserInWhatSecurityDomain;
                    }
                    else
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "The initialization of the property CreateUserInWhatSecurityDomain failed. The security domain created was null. The field: '{0}' contains the value  '{1}'.",
                                           FieldCreateUserInWhatSecurityDomain, createUserInWhatSecurityDomainString));
                    }
                }
                else
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The initialization of the property CreateUserInWhatSecurityDomain failed. The security domain provided does not exist. The field: '{0}' contains the value '{1}'.",
                                       FieldCreateUserInWhatSecurityDomain, createUserInWhatSecurityDomainString));
                }
            }
            else
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "The initialization of the property CreateUserInWhatSecurityDomain failed. The value provided in the field: '{0}' was null or empty. Value: '{1}'.",
                                   FieldCreateUserInWhatSecurityDomain, createUserInWhatSecurityDomainString));
            }

            string addUserToWhatStandardRoles = importItem.Fields[FieldAddUserToWhatStandardRoles].Value;
            string errorMessage;
            AddUserToWhatStandardRoles = InitializeRolesListFromPipeseperatedString(FieldAddUserToWhatStandardRoles,
                                                                                    addUserToWhatStandardRoles,
                                                                                    out errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                LogBuilder.Log("Error", errorMessage);
            }

            GetPasswordFromWhatField = importItem.Fields["Get Password From What Field"].Value;
            AutogeneratePasswordWithLength = 7;
            var autogeneratePasswordWithLength = importItem.Fields["Autogenerate Password With Length"].Value;
            if (!String.IsNullOrEmpty(autogeneratePasswordWithLength))
            {
                int length;
                if (!Int32.TryParse(autogeneratePasswordWithLength, out length))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The field '{0}' must contain a integer that states the length of the password. Value: {1}.",
                                       "Autogenerate Password With Length", autogeneratePasswordWithLength));
                }
                else
                {
                    AutogeneratePasswordWithLength = length;
                }
            }

            // Users Present In Import
            string onPresentInImportAddUserToRoles = importItem.Fields[FieldOnPresentInImportAddToRoles].Value;
            OnPresentInImportAddToRoles = InitializeRolesListFromPipeseperatedString(FieldOnPresentInImportAddToRoles,
                                                                                     onPresentInImportAddUserToRoles,
                                                                                     out errorMessage, true);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                LogBuilder.Log("Error", errorMessage);
            }

            string onPresentInImportRemoveUserFromRoles = importItem.Fields[FieldOnPresentInImportRemoveFromRoles].Value;
            OnPresentInImportRemoveFromRoles =
                InitializeRolesListFromPipeseperatedString(FieldOnPresentInImportRemoveFromRoles,
                                                           onPresentInImportRemoveUserFromRoles, out errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                LogBuilder.Log("Error", errorMessage);
            }

            // Users Not Present In Import
            IsProcessUsersNotPresentInImport = importItem["Process Users Not Present In Import"] == "1";

            string onNotPresentInImportAddToRoles = importItem.Fields[FieldOnNotPresentInImportAddToRoles].Value;
            OnNotPresentInImportAddToRoles =
                InitializeRolesListFromPipeseperatedString(FieldOnNotPresentInImportAddToRoles,
                                                           onNotPresentInImportAddToRoles, out errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                LogBuilder.Log("Error", errorMessage);
            }

            string onNotPresentInImportRemoveFromRoles =
                importItem.Fields[FieldOnNotPresentInImportRemoveFromRoles].Value;
            OnNotPresentInImportRemoveFromRoles =
                InitializeRolesListFromPipeseperatedString(FieldOnNotPresentInImportRemoveFromRoles,
                                                           onNotPresentInImportRemoveFromRoles, out errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                LogBuilder.Log("Error", errorMessage);
            }

            IsDoNotLogProgressStatusMessagesInSitecoreLog =
                importItem["Do Not Log Progress Status Messages In Sitecore Log"] == "1";

            SetProfileItemOnUser = importItem["Set ProfileItem On User"] == "1";
            UseWhatProfileItemId = importItem.Fields["Use What ProfileItemId"].Value;
            CheckThatCustomPropertyExistOnUserProfile = importItem["Check That Property Exist On User Profile"] == "1";

            IsDeleteUsersWithMembershipInStandardRolesOnly =
                importItem["Delete Users With Membership In Standard Roles Only"] == "1";

            //start handling fields
            Item fieldsRootItem = GetItemByTemplate(importItem, Utility.Constants.FieldsFolderID);
            if (fieldsRootItem.IsNotNull())
            {
                ChildList c = fieldsRootItem.GetChildren();
                if (c.Any())
                {
                    foreach (Item child in c)
                    {
                        var fieldDefinition = CreateFieldDefinition(child);
                        if (fieldDefinition != null)
                        {
                            FieldDefinitions.Add(fieldDefinition);
                        }
                    }
                }
                else
                {
                    LogBuilder.Log("Warn", "There are no fields to import");
                }
            }
            else
            {
                LogBuilder.Log("Warn", "There is no 'Fields' folder");
            }
        }

        private void InitializeUserKeyHandler()
        {
            if (IdentifyUserUniqueByFieldDefinition == null)
            {
                UserKeyHandler = new UserNameAsKeyHandler(this);
            }
            else
            {
                if (IdentifyUserUniqueByFieldDefinition.FieldStorageHandler != null)
                {
                    var userKeyStorage = IdentifyUserUniqueByFieldDefinition.FieldStorageHandler.UserKeyStorage;
                    if (userKeyStorage is MobileAliasUserKeyStorage)
                    {
                        UserKeyHandler = new MobileAliasUserKeyHandler(this, IdentifyUserUniqueByFieldDefinition);
                    }
                    else if (userKeyStorage is MobilePinUserKeyStorage)
                    {
                        UserKeyHandler = new MobilePinUserKeyHandler(this, IdentifyUserUniqueByFieldDefinition);
                    }
                    else
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "The initialization fo the UserKeyHandler failed, because the retrieved UserKeyStorage type was not of the expected types - either MobileAliasUserKeyStorage or MobilePinUserKeyStorage. " +
                                           "The process was aborted."));
                    }
                }
                else
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The initialization of the UserKeyHandler failed, because the retrieved IdentifyUserUniqueByFieldDefinition.FieldStorageHandler object was null. " +
                                       " A field should always have a fieldStorageHandler or else the field cannot save the value. " +
                                       "The process was aborted."));
                }
            }
        }

        private void InitializeIdentifyUserUniqueByFieldDefinition(Item importItem)
        {
            var identifyUserUniqueFromWhatField = importItem.Fields["Identify User Unique From What Field"].Value;
            if (!ID.IsID(identifyUserUniqueFromWhatField)) return;
            var fieldItem = SitecoreDB.GetItem(new ID(identifyUserUniqueFromWhatField));
            if (fieldItem != null)
            {
                var keyFieldDefinition = CreateFieldDefinition(fieldItem);
                if (keyFieldDefinition != null)
                {
                    IdentifyUserUniqueByFieldDefinition = keyFieldDefinition;
                }
            }
            else
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "The initialization of the property IdentifyUserUniqueByFieldDefinition failed. The fieldItem was not retrieved even though the ID was a correct Sitecore ID. " +
                                   "Value in field: {0}. ", identifyUserUniqueFromWhatField));
            }
        }

        private List<Role> InitializeRolesListFromPipeseperatedString(string settingName, string roleNamesString,
                                                                      out string errorMessage, bool isRequired = false)
        {
            List<Role> roles = new List<Role>();
            errorMessage = String.Empty;

            if (!String.IsNullOrEmpty(roleNamesString))
            {
                var roleNames = roleNamesString.Split('|');
                if (roleNames.Any())
                {
                    foreach (string roleName in roleNames)
                    {
                        if (Role.Exists(roleName))
                        {
                            var role = Role.FromName(roleName);
                            if (role != null)
                            {
                                roles.Add(role);
                            }
                            else
                            {
                                errorMessage +=
                                    string.Format(
                                        "An error occured when trying to create the Role from the Rolename - it was null. RoleName: {0}. Please verify the value in the field '{1}'. The error occured in the InitializeRolesListFromPipeseperatedString(). roleNameString: {2}.",
                                        settingName, roleName, roleNamesString);
                            }
                        }
                        else
                        {
                            errorMessage +=
                                string.Format(
                                    "The specified role doesn't not exist in the Membership database. Please verify the value in the field '{0}'. The error occured in the InitializeRolesListFromPipeseperatedString(). Role: {1}. roleNameString: {2}.",
                                    settingName, roleName, roleNamesString);
                        }
                    }
                }
                else
                {
                    if (isRequired)
                    {
                        errorMessage +=
                            string.Format(
                                "The defined roleNameString was defined, but didn't result in any roleNames after splitting of the string with pipe. Please provide a value or validate the field '{0}' for the roles. The error occured in the InitializeRolesListFromPipeseperatedString(). roleNameString: {1}.",
                                settingName, roleNamesString);

                    }
                }
            }
            else
            {
                if (isRequired)
                {
                    errorMessage +=
                        string.Format(
                            "The defined roleNameString was null or empty. This value is required. Please provide a value in the field '{0}' for the roles. The error occured in the InitializeRolesListFromPipeseperatedString(). roleNameString: {1}.",
                            settingName, roleNamesString);

                }
            }
            if (isRequired && !roles.Any())
            {
                errorMessage +=
                    string.Format(
                        "The List<Role> was empty, but is set to be required. This doesn't work out. Please provide a value in the field '{0}' for the roles. This value is required. The error occured in the InitializeRolesListFromPipeseperatedString(). roleNameString: {1}.",
                        settingName, roleNamesString);
            }
            return roles;
        }

        #endregion Constructor

        private IBaseField CreateFieldDefinition(Item fieldItem)
        {
            BaseMapping bm = new BaseMapping(this, fieldItem);
            if (!string.IsNullOrEmpty(bm.HandlerAssembly))
            {
                if (!string.IsNullOrEmpty(bm.HandlerClass))
                {
                    //create the object from the class and cast as base field to add it to field definitions
                    try
                    {
                        var bf =
                            (IBaseField)
                            Sitecore.Reflection.ReflectionUtil.CreateObject(bm.HandlerAssembly, bm.HandlerClass,
                                                                            new object[] {this, fieldItem});
                        if (bf != null)
                        {
                            return bf;
                        }
                        LogBuilder.Log("Error",
                                       string.Format("the field: '{0}' class type {1} could not be instantiated",
                                                     fieldItem.Name, bm.HandlerClass));
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        LogBuilder.Log("Error",
                                       string.Format(
                                           "the field:{0} binary {1} specified could not be found. Exception: {2}.",
                                           fieldItem.Name, bm.HandlerAssembly, GetExceptionDebugInfo(fnfe)));
                    }
                }
                else
                {
                    LogBuilder.Log("Error",
                                   string.Format("the field: '{0}' Handler Class {1} is not defined", fieldItem.Name,
                                                 bm.HandlerClass));
                }
            }
            else
            {
                LogBuilder.Log("Error",
                               string.Format("the field: '{0}' Handler Assembly {1} is not defined", fieldItem.Name,
                                             bm.HandlerAssembly));
            }
            return null;
        }

        #region Abstract Methods

        /// <summary>
        /// gets the data to be imported
        /// </summary>
        /// <returns></returns>
        public abstract IList<object> GetImportData();

        public virtual void ValidateImportData(IList<object> importData, ref string errorMessage)
        {
            var keyList = new Dictionary<string, object>();
            var keyErrors = 0;
            foreach (var importRow in importData)
            {
                string importRowErrorMessage = "";
                string keyValue = GetKeyValueFromImportRow(importRow, ref importRowErrorMessage);
                if (!String.IsNullOrEmpty(importRowErrorMessage))
                {
                    errorMessage +=
                        String.Format(
                            "--- An error occured trying to validate if the key was unique in the importData. ErrorMessage: {0}. ImportRow: {1}.\r\n",
                            importRowErrorMessage, GetImportRowDebugInfo(importRow));
                    keyErrors++;
                    continue;
                }
                if (String.IsNullOrEmpty(keyValue))
                {
                    errorMessage +=
                        String.Format("--- The keyValue was null or empty in the importData. ImportRow: {0}.\r\n",
                                      GetImportRowDebugInfo(importRow));
                    keyErrors++;
                    continue;
                }
                if (!keyList.ContainsKey(keyValue))
                {
                    keyList.Add(keyValue, importRow);
                }
                else if (!IsInValidationAllowDuplicatesInKey)
                {
                    errorMessage +=
                        String.Format("--- There were duplicate keyValue's in the importData. ImportRow: {0}.\r\n",
                                      GetImportRowDebugInfo(importRow));
                    keyErrors++;
                }
            }
            if (keyErrors > 0)
            {
                errorMessage = String.Format("Validation found {0} duplicate keys errors.\r\n", keyErrors) +
                               errorMessage;
            }
        }

        /// <summary>
        /// this is used to process custom fields or properties
        /// </summary>
        public abstract bool ProcessCustomData(ref User user, object importRow, out bool processedCustomData);

        /// <summary>
        /// Defines how the subclass will retrieve a field value
        /// </summary>
        public abstract string GetFieldValue(object importRow, string fieldName, ref string errorMessage);

        public abstract string GetImportRowDebugInfo(object importRow);

        public virtual string GetItemDebugInfo(Item item)
        {
            if (item != null)
            {
                return item.Paths.ContentPath.Replace("sitecore/content", "") + " (" + item.ID + ")";
            }
            return string.Empty;
        }

        public virtual string GetExceptionDebugInfo(Exception exception)
        {
            if (exception != null)
            {
                string log = "Message: " + exception.Message + ".\r\nSource: " + exception.Source
                             + "\r\nStacktrace: " + exception.StackTrace;
                if (exception.InnerException != null)
                {
                    log += "\r\nInnerException:\r\n" + GetExceptionDebugInfo(exception.InnerException);
                }
                return log;
            }
            return "Exception was null";
        }

        public virtual string GetItemListDebugInfo(User[] userList)
        {
            string debugInfo = "";
            for (int i = 0; i < userList.Count(); i++)
            {
                User user = userList[i];
                if (user != null)
                {
                    debugInfo += GetUserDebugInfo(user);
                }
                if (i != userList.Count() - 1)
                {
                    debugInfo += ", ";
                }
            }
            return debugInfo;
        }

        public virtual string GetItemListDebugInfo(List<User> userList)
        {
            string debugInfo = "";
            for (int i = 0; i < userList.Count(); i++)
            {
                User item = userList[i];
                if (item != null)
                {
                    debugInfo += GetUserDebugInfo(item);
                }
                if (i != userList.Count() - 1)
                {
                    debugInfo += ", ";
                }
            }
            return debugInfo;
        }

        public virtual string GetUserDebugInfo(User user)
        {
            if (user != null)
            {
                return user.LocalName + " (" + user.Name + ")";
            }
            return string.Empty;
        }

        #endregion Abstract Methods

        #region Static Methods

        /// <summary>
        /// will begin looking for or creating date folders to get a parent node to create the new items in
        /// </summary>
        /// <param name="parentNode">current parent node to create or search folder under</param>
        /// <param name="dt">date time value to folder by</param>
        /// <param name="folderType">folder template type</param>
        /// <returns></returns>
        public static Item GetDateParentNode(Item parentNode, DateTime dt, TemplateItem folderType)
        {
            //get year folder
            Item year = parentNode.Children[dt.Year.ToString()];
            if (year == null)
            {
                //build year folder if you have to
                year = parentNode.Add(dt.Year.ToString(), folderType);
            }
            //set the parent to year
            parentNode = year;

            //get month folder
            Item month = parentNode.Children[dt.ToString("MM")];
            if (month == null)
            {
                //build month folder if you have to
                month = parentNode.Add(dt.ToString("MM"), folderType);
            }
            //set the parent to year
            parentNode = month;

            //get day folder
            Item day = parentNode.Children[dt.ToString("dd")];
            if (day == null)
            {
                //build day folder if you have to
                day = parentNode.Add(dt.ToString("dd"), folderType);
            }
            //set the parent to year
            parentNode = day;

            return parentNode;
        }

        /// <summary>
        /// will begin looking for or creating letter folders to get a parent node to create the new items in
        /// </summary>
        /// <param name="parentNode">current parent node to create or search folder under</param>
        /// <param name="letter">the letter to folder by</param>
        /// <param name="folderType">folder template type</param>
        /// <returns></returns>
        public static Item GetNameParentNode(Item parentNode, string letter, TemplateItem folderType)
        {
            //get letter folder
            Item letterItem = parentNode.Children[letter];
            if (letterItem == null)
            {
                //build year folder if you have to
                letterItem = parentNode.Add(letter, folderType);
            }
            //set the parent to year
            return letterItem;
        }

        #endregion Static Methods

        #region Methods

        protected virtual string GetKeyValueFromImportRow(object importRow, ref string errorMessage)
        {
            return UserKeyHandler.GetKeyValueFromImportRow(importRow, ref errorMessage);
        }

        protected virtual string GetKeyValueFromUser(User user, ref string errorMessage)
        {
            return UserKeyHandler.GetKeyValueFromUser(user, ref errorMessage);
        }

        protected virtual List<User> GetUsersByKeyValue(string keyValue, ref string errorMessage)
        {
            return UserKeyHandler.GetUsersByKeyValue(keyValue, ref errorMessage);
        }

        protected virtual Dictionary<string, User> GetUsersByRoles(IEnumerable<Role> roles, ref string errorMessage)
        {
            var list = new Dictionary<string, User>();
            try
            {
                // Find user in core database
                foreach (var role in roles)
                {
                    var usersInRole = RolesInRolesManager.GetUsersInRole(role, false);
                    if (usersInRole != null)
                    {
                        foreach (var user in usersInRole)
                        {
                            if (user != null)
                            {
                                if (!list.ContainsKey(user.LocalName))
                                {
                                    list.Add(user.LocalName, user);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage +=
                    String.Format(
                        "The GetUsersByRoles thrown an exception in trying to query the item. Exception: {0}",
                        GetExceptionDebugInfo(ex));
                return null;
            }
            return list;
        }

        /// <summary>
        /// processes each field against the data provided by subclasses
        /// </summary>
        public virtual Logging Process()
        {
            string importIdentifier = String.Format("{0} - {1}",
                                                    String.Format(DateTime.Now.ToLongDateString(), "dd-MMM-yyyy") + " " +
                                                    String.Format(DateTime.Now.ToLongTimeString(), "hh:mm:ss"),
                                                    ImportItem.Name);
            Diagnostics.Log.Info(String.Format("UserSync job started - {0}.", importIdentifier), typeof (BaseDataMap));
            //if log messages then don't start Process method.
            if (LogBuilder.LogBuilder.Length >= 1)
            {
                LogBuilder.Log("Error", "The import did not run.");
                return LogBuilder;
            }
            IList<object> importedRows;
            try
            {
                importedRows = GetImportData();
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Connection Error in Process method.", GetExceptionDebugInfo(ex));
                return LogBuilder;
            }

            if (importedRows == null)
            {
                LogBuilder.Log("Error",
                               "The GetImportData method returned a null object. Therefore the import was not performed.");
                return LogBuilder;
            }

            if (!LimitImportRows(importedRows.Count(), ref importedRows))
            {
                return LogBuilder;
            }

            int numberOfRows = importedRows.Count();
            LogBuilder.TotalNumberOfUsers = numberOfRows;
            if (numberOfRows < GetMinimumNumberOfRowsRequiredToStartImport())
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "The GetImportData method encounted that the number of rows in import was lower than the minimum number of rows required. Therefore the import wasn't started. This value is defined in the GetMinimumNumberOfRowsRequiredToStartImport method. This value can be changed by the field 'Minimum Number Of Rows Required To Run The Import' on the import item or by overwriting the method in a custom DataMap object. Therefore the import was not performed. MinimumNumberOfRowsRequiredToStartImport: {0}. NumberOfRows: {1}.",
                                   GetMinimumNumberOfRowsRequiredToStartImport(), numberOfRows));
                return LogBuilder;
            }
            if (IsValidateImportDataBeforeProcessing)
            {
                string errorMessage = "";
                ValidateImportData(importedRows, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "An validation of the Import Data resultet in errors. See the following detailed errormessages:\r\n\r\n{0}",
                                       errorMessage));
                    return LogBuilder;
                }
            }

            int minimumNumberOfRowsRequiredToStartImport = GetNumberOfRowsToProcessBeforeLogStatus(numberOfRows);
            if (minimumNumberOfRowsRequiredToStartImport < 1)
            {
                minimumNumberOfRowsRequiredToStartImport = DefaultNumberOfRowsToProcessBeforeLogStatus;
            }

            //Loop through the data source
            foreach (object importRow in importedRows)
            {
                if (!IsDoNotLogProgressStatusMessagesInSitecoreLog)
                {
                    if (LogBuilder.ProcessedUsers%minimumNumberOfRowsRequiredToStartImport == 0)
                    {
                        Diagnostics.Log.Info(
                            String.Format("UserSync job - {0} - processed. \r\n{1}", importIdentifier,
                                          LogBuilder.GetStatusText()), typeof (BaseDataMap));
                    }
                }
                LogBuilder.ProcessedUsers += 1;
                if (!ProcessImportRow(importRow))
                {
                    continue;
                }
                LogBuilder.SucceededUsers += 1;
            }

            // Process Users Not Present In Import
            if (IsProcessUsersNotPresentInImport)
            {
                ProcessUsersNotPresentInImport(importedRows);
            }
            Diagnostics.Log.Info(
                String.Format("UserSync job - {0} ended. {1}.", importIdentifier, LogBuilder.GetStatusText()),
                typeof (BaseDataMap));
            return LogBuilder;
        }

        private bool LimitImportRows(int numberOfRows, ref IList<object> importedRows)
        {
            try
            {
                IList<object> subImportedRows = new List<object>();
                if (StartOnWhatImportRowIndex < numberOfRows)
                {
                    var endIndex = StartOnWhatImportRowIndex + LimitNumberOfImportRowsToWhatNumber;
                    if (LimitNumberOfImportRowsToWhatNumber != 0 && endIndex != 0)
                    {
                        if (endIndex < numberOfRows && endIndex > StartOnWhatImportRowIndex)
                        {
                            for (var i = StartOnWhatImportRowIndex; i < endIndex && i < importedRows.Count(); i++)
                            {
                                var importedRow = importedRows[i];
                                subImportedRows.Add(importedRow);
                            }
                            if (subImportedRows.Any())
                            {
                                importedRows = subImportedRows;
                            }
                        }
                        else
                        {
                            LogBuilder.Log("Error",
                                           String.Format(
                                               "The range to limit the list on was out of range and the import was aborted. StartOnWhatImportRowIndex: {0}. LimitNumberOfImportRowsToWhatNumber: {1}. NumberOfRows: {2}."
                                               , StartOnWhatImportRowIndex, LimitNumberOfImportRowsToWhatNumber,
                                               numberOfRows));
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "An error occured while trying to limit the ImportRows to the spesified StartOnWhatImportRowIndex and LimitNumberOfImportRowsToWhatNumber. " +
                                   "The process was aborted. StartOnWhatImportRowIndex: {0}. LimitNumberOfImportRowsToWhatNumber: {1}. NumberOfRows: {2}. Exception: {3}."
                                   , StartOnWhatImportRowIndex, LimitNumberOfImportRowsToWhatNumber, numberOfRows,
                                   GetExceptionDebugInfo(ex)));
                return false;
            }
            return true;
        }

        protected virtual bool ProcessImportRow(object importRow)
        {
            try
            {
                // The user to process
                User user = null;

                string errorMessage = String.Empty;
                var keyValue = GetKeyValueFromImportRow(importRow, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error", String.Format(
                        "The value defined 'Identify User Unique From What Field' didn't result in any value on the import row. This field is used to identify the user unique. Therefore the following user wasn't imported: {0}. The errorMessage was: {1}",
                        GetImportRowDebugInfo(importRow), errorMessage));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }

                var users = GetUsersByKeyValue(keyValue, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage) || users == null)
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "An error occured trying to determine if the user with the key: '{0}' exists before. The processing of that user was aborted. The error happend in GetUsersByKey method. ImportRow: {1}. THe errorMessage was: {2}",
                                       keyValue, GetImportRowDebugInfo(importRow), errorMessage));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }
                if (users.Count() > 1)
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "There were more than one user with the same key. The key must be unique. Therefore the following user wasn't imported. Key: {0}. Items: {1}. ImportRow: {2}.",
                                       keyValue, GetItemListDebugInfo(users), GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }
                // The user exists before
                if (users.Count == 1)
                {
                    user = users.First();
                }

                // Create a new user
                if (user == null)
                {
                    if (!CreateUser(importRow, ref user, keyValue))
                    {
                        return false;
                    }
                }
                // Update the user
                if (!UpdateUser(user, importRow))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "An exception occured in Process method in foreach (object importRow in importItems). The processing of the importRow was aborted. ImportRow: '{0}'. Exception: {1}",
                                   GetImportRowDebugInfo(importRow), GetExceptionDebugInfo(ex)));
                LogBuilder.FailureUsers += 1;
                return false;
            }
            return true;
        }

        protected virtual int GetMinimumNumberOfRowsRequiredToStartImport()
        {
            return MinimumNumberOfRowsRequiredToRunTheImport;
        }

        protected virtual int GetNumberOfRowsToProcessBeforeLogStatus(int numberOfRowsTotal)
        {
            if (numberOfRowsTotal > 0)
            {
                return numberOfRowsTotal/10;
            }
            return DefaultNumberOfRowsToProcessBeforeLogStatus;
        }

        protected virtual void ProcessUsersNotPresentInImport(IList<object> importRows)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    string errorMessage = String.Empty;
                    var usersCurrentlyMembersOfRoles = GetUsersByRoles(OnPresentInImportAddToRoles, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "In the ProcessUsersNotPresentInImport method the GetUsersByRoles failed. The disabling process was terminated. {0}.",
                                           errorMessage));
                        return;
                    }
                    var usersKeyList = GetUsersKeyList(importRows, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "In the ProcessUsersNotPresentInImport method the GetUsersKeyList failed. The disabling process was terminated. {0}.",
                                           errorMessage));
                        return;
                    }

                    if (usersCurrentlyMembersOfRoles != null && usersCurrentlyMembersOfRoles.Any())
                    {
                        LogTotalNumberOfNotPresentInImportUsers(usersCurrentlyMembersOfRoles, usersKeyList);
                        foreach (var user in usersCurrentlyMembersOfRoles.Values)
                        {
                            string userErrorMessage = String.Empty;
                            try
                            {
                                var keyValue = GetKeyValueFromUser(user, ref userErrorMessage);
                                if (!String.IsNullOrEmpty(userErrorMessage))
                                {
                                    LogBuilder.Log("Error",
                                                   String.Format(
                                                       "In the ProcessUsersNotPresentInImport method in the foreach (var user in usersCurrentlyMembersOfRoles) an error occured in the GetKeyValueFromUser method. The process was terminated for that user. The rest of the process continued. {0}.",
                                                       userErrorMessage));
                                    LogBuilder.FailedNotPresentInImportProcessedUsers += 1;
                                    continue;
                                }
                                // If the keyValue is null or empty we do want to process that user anyway, as well as users with a key not present in the import
                                if (String.IsNullOrEmpty(keyValue) || !usersKeyList.ContainsKey(keyValue))
                                {
                                    ProcessUserNotPresentInImport(user, ref userErrorMessage);
                                    if (String.IsNullOrEmpty(userErrorMessage))
                                    {
                                        LogBuilder.NotPresentInImportProcessedUsers += 1;
                                    }
                                    else
                                    {
                                        LogBuilder.Log("Error",
                                                       String.Format(
                                                           "In the ProcessUsersNotPresentInImport method in the foreach (var user in usersCurrentlyMembersOfRoles) in the ProcessUserNotPresentInImport method an error occured. The process was terminated for that user. The rest of the process continued. {0}.",
                                                           userErrorMessage));
                                        LogBuilder.FailedNotPresentInImportProcessedUsers += 1;
                                        continue;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogBuilder.Log("Error",
                                               String.Format(
                                                   "An exception occured in Process method when running the ProcessUsersNotPresentInImport in foreach (var user in usersCurrentlyMembersOfRoles). Item: {0}. Exception: {1}",
                                                   GetUserDebugInfo(user), GetExceptionDebugInfo(ex)));
                                LogBuilder.FailedNotPresentInImportProcessedUsers += 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "An exception occured in Process method When Disabling Items Not Present In Import. Exception: {0}",
                                   GetExceptionDebugInfo(ex)));
            }
        }

        private void LogTotalNumberOfNotPresentInImportUsers(Dictionary<string, User> usersCurrentlyMembersOfRoles,
                                                             IDictionary<string, object> usersKeyList)
        {
            var totalNumberOfNotPresentInImportUsers = usersCurrentlyMembersOfRoles.Count -
                                                       usersKeyList.Count;
            LogBuilder.TotalNumberOfNotPresentInImportUsers = totalNumberOfNotPresentInImportUsers;
        }

        protected virtual void ProcessUserNotPresentInImport(User user, ref string errorMessage)
        {
            if (user != null)
            {
                foreach (var role in OnNotPresentInImportAddToRoles)
                {
                    if (!user.IsInRole(role))
                    {
                        user.Roles.Add(role);
                    }
                }
                foreach (var role in OnNotPresentInImportRemoveFromRoles)
                {
                    if (user.IsInRole(role))
                    {
                        user.Roles.Remove(role);
                    }
                }
                if (IsDeleteUsersWithMembershipInStandardRolesOnly)
                {
                    DeleteUsersWithMembershipInStandardRolesOnly(user, ref errorMessage);
                }
            }
            else
            {
                errorMessage +=
                    String.Format(
                        "The user was null. Therefore the ProcessUserNotPresentInImport method could not be completed.");
            }
        }

        protected virtual void DeleteUsersWithMembershipInStandardRolesOnly(User user, ref string errorMessage)
        {
            if (user != null)
            {
                try
                {
                    bool userContainsOtherThanStandardRoles = false;

                    foreach (var role in user.Roles)
                    {
                        if (!AddUserToWhatStandardRoles.Contains(role))
                        {
                            userContainsOtherThanStandardRoles = true;
                            break;
                        }
                    }
                    if (!userContainsOtherThanStandardRoles)
                    {
                        if (ExecuteActionOnUserNotPresentInImport(user, ref errorMessage))
                        {
                            LogBuilder.DeletedUsers += 1;
                            return;
                        }
                        else
                        {
                            LogBuilder.FailedDeletedUsers += 1;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage +=
                        String.Format(
                            "An error occured in the DeleteUsersWithMembershipInStandardRolesOnly method. Exception: {0}.",
                            ex.Message);
                    LogBuilder.FailedDeletedUsers += 1;
                    return;
                }
            }
            else
            {
                errorMessage +=
                    String.Format("The user was null in the DeleteUsersWithMembershipInStandardRolesOnly method.");
                LogBuilder.FailedDeletedUsers += 1;
                return;
            }
        }

        protected virtual bool ExecuteActionOnUserNotPresentInImport(User user, ref string errorMessage)
        {
            if (user != null)
            {
                user.Delete();
                return true;
            }
            errorMessage += "The user was null in the ExecuteActionOnUserNotPresentInImport method. The user was not processed. ";
            return false;
        }

        protected virtual IDictionary<string, object> GetUsersKeyList(IEnumerable<object> importRows, ref string errorMessage)
        {
            var list = new Dictionary<string, object>();
            foreach (object importRow in importRows)
            {
                var value = GetKeyValueFromImportRow(importRow, ref errorMessage);
                if (!String.IsNullOrEmpty(value) &&
                    !list.ContainsKey(value))
                {
                    list.Add(value, importRow);
                }
            }
            return list;
        }

        protected virtual string GetPasswordForUser(object importRow, ref User user, ref string errorMessage)
        {
            string password = String.Empty;
            if (!String.IsNullOrEmpty(GetPasswordFromWhatField))
            {
                password = GetFieldValue(importRow, GetPasswordFromWhatField, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    errorMessage += String.Format("An error occured trying to retrieve the password from the importRow for an user in the GetPasswordForUser method. " +
                                                  "Password: {0}. ErrorMessage: {1}. ImportRow: {2}. User: {3}.", password, errorMessage, GetImportRowDebugInfo(importRow), GetUserDebugInfo(user));
                    return null;
                }
                // If the password is null or empty, a autogenerated password will be created.
            }

            if (String.IsNullOrEmpty(password))
            {
                int passwordLength = AutogeneratePasswordWithLength;
                if (passwordLength < 5 || passwordLength > 30)
                {
                    passwordLength = 7;
                }
                password = GeneratePassword(passwordLength);
                if (String.IsNullOrEmpty(password))
                {
                    errorMessage +=
                        String.Format(
                            "The password was null or empty in the GetPasswordForUser. The password must be set when creating a user. ImportRow: {0}. User: {1}.",
                            GetImportRowDebugInfo(importRow), GetUserDebugInfo(user));
                    return null;
                }
            }
            return password;
        }

        private string GeneratePassword(int passwordLength = 7)
        {
            const string strPwdChar = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var strPwd = String.Empty;
            var rnd = new Random();
            for (var i = 0; i < passwordLength; i++)
            {
                var iRandom = rnd.Next(0, strPwdChar.Length - 1);
                strPwd += strPwdChar.Substring(iRandom, 1);
            }
            return strPwd;
        }

        protected virtual bool CreateUser(object importRow, ref User user, string keyValue)
        {
            try
            {
                var errorMessage = String.Empty;
                var userName = GetUserName(importRow, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "An error occured during generation of username. Therefore the user could not be created. This happened in the Process method in foreach (object importRow in importedRows). Errors: {0}. ImportRow: {1}.",
                                       errorMessage, GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }

                if (String.IsNullOrEmpty(userName))
                {
                    LogBuilder.Log("Error",
                        String.Format(
                            "The userName could not be parsed for importRow: {0}. Therefore the user could not be created.",
                            GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }
                var fullName = CreateUserInWhatSecurityDomain.GetFullName(userName);
                // Create user
                if (User.Exists(fullName))
                {
                    var existingUser = User.FromName(fullName, true);
                    string existingUserKey = String.Empty;
                    if (existingUser != null)
                    {
                        existingUserKey = GetKeyValueFromUser(existingUser, ref errorMessage);
                    }
                    LogBuilder.Log("Error", String.Format("The CreateUser method failed because a user with that userName already exists, but with a different key. The key on the existing user: '{0}'. " +
                                                          "The user could not be created. This can happen if the key for the user is not the username. " +
                                                          "ExistingUser: {1}. ImportRow: {2}. UserName: {3}. Error: {4}.", existingUserKey, GetUserDebugInfo(existingUser), GetImportRowDebugInfo(importRow), userName, errorMessage));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }
                else
                {
                    if (!String.IsNullOrEmpty(fullName))
                    {
                        string password = GetPasswordForUser(importRow, ref user, ref errorMessage);
                        if (!String.IsNullOrEmpty(errorMessage))
                        {
                            LogBuilder.Log("Error", String.Format("The GetPasswordForUser returned an error. " +
                                                                  "This means that the user is not correctly created. " +
                                                                  "The import of that user was aborted. " +
                                                                  "ErrorMessage: {0}. ImportRow: {1}.", errorMessage, GetImportRowDebugInfo(importRow)));
                            LogBuilder.FailureUsers += 1;
                            return false;
                        }
                        user = User.Create(fullName, password);
                    }
                    else
                    {
                        LogBuilder.Log("Error", String.Format("The CreateUser method failed because the fullName returned from Domain.GetFullName() was null or empty. ImportRow: {0}. UserName: {1}. Domain: {2}. FullName: {3}.", GetImportRowDebugInfo(importRow), userName, CreateUserInWhatSecurityDomain.Name, fullName));
                        LogBuilder.FailureUsers += 1;
                        return false;
                    }
                }

                if (user == null)
                {
                    LogBuilder.Log("Error", String.Format("The new user created was null. ImportRow: {0}.", GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }
                
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error", String.Format("The new user was created, but it failed in setting the unique KeyValue for the user in the SetValueToIdentifyTheSameUsersBy method with an errormessage. " +
                                                          "This means that the user is not correctly created and is not possible to retrieve later on the basis of the key. " +
                                                          "The import of that user was aborted. The user was not removed again." +
                                                          "User: {0}. ImportRow: {1}. ErrorMessage: {2}.", GetUserDebugInfo(user), GetImportRowDebugInfo(importRow), errorMessage));
                    LogBuilder.FailureUsers += 1;
                    return false;
                }

                AddUserToStandardRoles(user, importRow);

                LogBuilder.CreatedUsers += 1;
                return true;
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                    String.Format(
                        "An exception occured in CreateUser. Exception: {0}",
                        ex.Message)); 
                LogBuilder.FailureUsers += 1;
                return false;
            }
        }

        protected virtual void AddUserToStandardRoles(User user, object importRow)
        {
            // Add standardroles    
            foreach (var role in AddUserToWhatStandardRoles)
            {
                if (!user.IsInRole(role))
                {
                    user.Roles.Add(role);
                }
            }
        }

        /// <summary>
        /// searches under the parent for an item whose template matches the id provided
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public Item GetItemByTemplate(Item parent, string templateId)
        {
            IEnumerable<Item> x = from Item i in parent.GetChildren()
                                  where i.Template.IsID(templateId)
                                  select i;
            return (x.Any()) ? x.First() : null;
        }

        protected virtual bool UpdateUser(User user, object importRow)
        {
            try
            {
                // Add in the field mappings
                var updatedFields = false;
                var failedItem = false;

                if (!UpdateProfileItemOnUser(user, ref updatedFields)) return false;

                var updatedRoles = UpdateRolesOnUser(user, importRow);
                if (updatedRoles)
                {
                    LogBuilder.UpdatedRolesUsers++;
                }

                foreach (IBaseField fieldDefinition in FieldDefinitions)
                {
                    string errorMessage = String.Empty;
                    var existingFieldNames = fieldDefinition.GetExistingFieldNames();
                    var fieldValueDelimiter = fieldDefinition.GetFieldValueDelimiter();
                    IEnumerable<string> values = GetFieldValues(existingFieldNames, importRow, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        LogBuilder.Log("Error", String.Format("An error occured in extracting the values from a specific field: '{0}' on the user: '{1}'. The processing of the user is aborted. ErrorMessage: {2}", 
                            fieldDefinition, GetUserDebugInfo(user), errorMessage));
                        LogBuilder.FailureUsers += 1;
                        return false;
                    }
                    bool updateField;
                    var status = fieldDefinition.FillField(this, importRow, ref user, String.Join(fieldValueDelimiter, values.ToArray()), out updateField);
                    if (!String.IsNullOrEmpty(status))
                    {
                        LogBuilder.Log("FieldError", String.Format("An error occured in processing a field on the user: '{0}'. The processing of the user in itself is not aborted and the rest of the fields has been processed. The error was: {1}", GetUserDebugInfo(user), status));
                        failedItem = true;
                    }
                    if (updateField)
                    {
                        // Must trigger the Save to store the values to the membership database.
                        using (new SecurityDisabler())
                        {
                            user.Profile.Save();
                        }
                        updatedFields = true;
                    }
                }
                if (updatedFields)
                {
                    LogBuilder.UpdatedFields += 1;
                }
                if (failedItem)
                {
                    LogBuilder.FailureUsers += 1;
                    return false;
                }

                // Calls the subclass method to handle custom fields and properties
                bool processedCustomData;
                if (!ProcessCustomData(ref user, importRow, out processedCustomData))
                {
                    LogBuilder.FailureUsers += 1;
                    return false;
                }
                if (processedCustomData)
                {
                    LogBuilder.ProcessedCustomDataUsers += 1;
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error", String.Format("An exception occured in UpdateUser. ImportRow: {0}. User: {1}, Exception: {2}", 
                    GetImportRowDebugInfo(importRow), GetUserDebugInfo(user), GetExceptionDebugInfo(ex))); 
                LogBuilder.FailureUsers += 1;
                return false;
            }
            return true;
        }

        private bool UpdateProfileItemOnUser(User user, ref bool updatedFields)
        {
            if (SetProfileItemOnUser)
            {
                string useWhatProfileItemId = DefaultUseWhatProfileItemId;
                if (!String.IsNullOrEmpty(UseWhatProfileItemId))
                {
                    useWhatProfileItemId = UseWhatProfileItemId;
                }
                if (user.Profile.ProfileItemId != useWhatProfileItemId)
                {
                    if (ID.IsID(useWhatProfileItemId))
                    {
                        user.Profile.ProfileItemId = useWhatProfileItemId;
                        updatedFields = true;
                    }
                    else
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "An error occured in updating the ProfileItemId property on the user.Profile. The value provided  was not a valid Sitecore ID." +
                                           " The value from was: '{0}'. The user was '{1}'. The processing of the user is aborted.",
                                           useWhatProfileItemId, GetUserDebugInfo(user)));
                        LogBuilder.FailureUsers += 1;
                        return false;
                    }
                }
            }
            return true;
        }

        protected virtual bool UpdateRolesOnUser(User user, object importRow)
        {
            bool updatedRoles = false;
            // Change roles membership
            foreach (var role in OnPresentInImportAddToRoles)
            {
                if (!user.IsInRole(role))
                {
                    user.Roles.Add(role);
                    updatedRoles = true;
                }
            }
            foreach (var role in OnPresentInImportRemoveFromRoles)
            {
                if (user.IsInRole(role))
                {
                    user.Roles.Remove(role);
                    updatedRoles = true;
                }
            }
            return updatedRoles;
        }

        /// <summary>
        /// creates an item name based on the name field values in the importRow
        /// </summary>
        protected virtual string GetUserName(object importRow, ref string errorMessage)
        {
            try
            {
                var userName = GetFieldValue(importRow, GetUsernameFromWhatField, ref errorMessage);
                if (String.IsNullOrEmpty(userName))
                {
                    errorMessage += String.Format("In method GetUserNameFromImport the userName was null or empty. UserName: {0}. ImportRow: '{1}'. GetUserNameFromWhatField: {2}.", userName, GetImportRowDebugInfo(importRow), GetUsernameFromWhatField);
                    return null;
                }
                return userName;
            }
            catch (Exception ex)
            {
                errorMessage += String.Format("In method GetUserNameFromImport an exception occured. ImportRow: '{0}'. GetUserNameFromWhatField: {1}. Exception: {2}.", GetImportRowDebugInfo(importRow), GetUsernameFromWhatField, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// retrieves all the import field values specified
        /// </summary>
        public IEnumerable<string> GetFieldValues(IEnumerable<string> fieldNames, object importRow, ref string errorMessage) {
            var list = new List<string>();
            foreach (string f in fieldNames) 
            {
                try
                {
                    var value = GetFieldValue(importRow, f, ref errorMessage);
                    if (value == null)
                    {
                        errorMessage += String.Format("In GetFieldValues method the field value was null. This should not happen. An empty string was added. FieldName: '{0}'", f);
                        list.Add(String.Empty);
                    }
                    else
                    {
                        list.Add(value.Trim());
                    }
                } 
                catch (ArgumentException ex) 
                {
                    if (string.IsNullOrEmpty(f))
                    {
                        errorMessage += String.Format("In GetFieldValues method the 'From' field name is empty. {0}", GetExceptionDebugInfo(ex));
                    }
                    else
                    {
                        errorMessage += String.Format("In GetFieldValues method the field name: '{0}' does not exist in the import row", f);
                    }
                }
            }
            return list;
        }

        #endregion Methods
	}
}
