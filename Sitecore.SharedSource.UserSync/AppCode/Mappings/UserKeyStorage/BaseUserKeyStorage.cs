using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;

namespace Sitecore.SharedSource.UserSync.AppCode.Mappings.UserKeyStorage
{
    public abstract class BaseUserKeyStorage
    {
        protected abstract string GetCommandPatternGetUserNameFromKey();

        protected abstract string GetCommandPatternGetKey();

        protected abstract string GetCommandPatternUpdateKey();

        public List<User> GetUsersFromKey(string key, string keyValue, ref string errorMessage)
        {
            var users = new List<User>();
            var connectionString = Configuration.Settings.GetConnectionString("core");
            if (!String.IsNullOrEmpty(connectionString))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string commandString = String.Format(GetCommandPatternGetUserNameFromKey(), key, keyValue);
                        using (var command = new SqlCommand(commandString, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                int count = 0;
                                while (reader.Read())
                                {
                                    count += 1;
                                    var userName = reader.GetString(0);
                                    if (!String.IsNullOrEmpty(userName)) 
                                    {
                                        var user = User.FromName(userName, true);
                                        if (user != null)
                                        {
                                            users.Add(user);
                                        }
                                        else
                                        {
                                            errorMessage +=
                                                String.Format(
                                                    "The SQL query returned a result with an userName, but the User could not be retrived, from method GetUserFromKey. A null object was returned. " +
                                                    "CommandString: {0}. keyValue: {1}. ConnectionString: {2}. UserName: {3}. Count: {4}.",
                                                    commandString, keyValue, connectionString, userName, count);
                                        }
                                    }
                                    else
                                    {
                                        errorMessage +=
                                            String.Format(
                                                "The SQL query returned a result with an username that was null or empty, from method GetUserFromKey. " +
                                                "CommandString: {0}. keyValue: {1}. ConnectionString: {2}. UserName: {3}. Count: {4}.",
                                                commandString, keyValue, connectionString, userName, count);
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
                            "An error occured in the GetUserFromKey method. See detailed errormessage. A null object was returned. keyValue: {0}. ConnectionString: {1}. Exception: {2}.",
                            keyValue, connectionString, ex);
                }
            }
            else
            {
                errorMessage +=
                        String.Format(
                            "An error occured in the GetUserFromKey method because the core connection string was empty. See detailed errormessage. keyValue: {0}. ConnectionString: {1}.",
                            keyValue, connectionString);
            }
            return users;
        }

        public string GetKeyValueFromUser(string userName, string key, ref string errorMessage)
        {
            string connectionString = Configuration.Settings.GetConnectionString("core");
            if (!String.IsNullOrEmpty(connectionString))
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string commandString = String.Format(GetCommandPatternGetKey(), key, userName);
                    using (var command = new SqlCommand(commandString, connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            string keyValue = result + "";
                            if (!String.IsNullOrEmpty(keyValue))
                            {
                                connection.Close();
                                return keyValue;
                            }
                        }
                    }
                }
            }
            else
            {
                errorMessage +=
                        String.Format(
                            "An error occured in the GetKeyValueFromUser method because the core connection string was empty. See detailed errormessage. ConnectionString: {0}.",
                            connectionString);
            }
            return null;
        }
        
        public int UpdateKeyValueToDotnetMemberShipProviderDatabase(string key, string keyValue, string userName, ref string errorMessage)
        {
            if (String.IsNullOrEmpty(key))
            {
                errorMessage +=
                    String.Format(
                        "The UpdateKeyValueToMemberShipDatabase failed because the key was null or empty. Since the key must be unique, this is an error. " +
                        "key: {0}. keyValue: {1}. userName: {2}.",
                        key, keyValue, userName);
            }
            if (String.IsNullOrEmpty(keyValue))
            {
                errorMessage +=
                    String.Format(
                        "The UpdateKeyValueToMemberShipDatabase failed because the keyValue was null or empty. Since the keyValue must be unique, this is an error. " +
                        "key: {0}. keyValue: {1}. userName: {2}.",
                        key, keyValue, userName);
            }
            else
            {
                if (keyValue.Length > 16)
                {
                    errorMessage +=
                        String.Format(
                            "The UpdateKeyValueToMemberShipDatabase failed because the keyValue was more than 16 characters, which is the limit of the column in the database. " +
                            "keyValue: {0}. userName: {1}.",
                            keyValue, userName);
                }
                else
                {
                    string currentKeyValue = GetKeyValueFromUser(userName, key, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        errorMessage +=
                            String.Format(
                                "The UpdateKeyValueToMemberShipDatabase failed because the GetKeyValueFromUser submethod failed in trying to get the key value from the user. " +
                                "The purpose was to verify wheether the keyvalue has changed. ErrorMessage: {0}." +
                                "CurrentKeyValue: {1}. key: {2}. keyValue: {3}. userName: {4}.",
                                errorMessage, currentKeyValue, key, keyValue, userName);
                    }

                    if (!keyValue.Equals(currentKeyValue))
                    {
                        var connectionString = Configuration.Settings.GetConnectionString("core");
                        if (!String.IsNullOrEmpty(connectionString))
                        {
                            using (var connection = new SqlConnection(connectionString))
                            {
                                connection.Open();
                                var commandString = String.Format(GetCommandPatternUpdateKey(), key, keyValue, userName);
                                using (var command = new SqlCommand(commandString, connection))
                                {
                                    int result = command.ExecuteNonQuery();
                                    if (result > 1)
                                    {
                                        errorMessage +=
                                            String.Format(
                                                "The UpdateKeyValueToMemberShipDatabase failed because more than one row was updated with the keyValue. Since the key must be unique, this is an error. " +
                                                "RowsAffected: {0}. keyValue: {1}. userName: {2}. connectionString: {3}.",
                                                result, keyValue, userName, connectionString);
                                        return result;
                                    }
                                    if (result < 0)
                                    {
                                        errorMessage +=
                                            String.Format(
                                                "The UpdateKeyValueToMemberShipDatabase failed because an error occured. " +
                                                "RowsAffected: {0}. keyValue: {1}. userName: {2}. connectionString: {3}.",
                                                result, keyValue, userName, connectionString);
                                        return result;
                                    }
                                    return result;
                                }
                            }
                        }
                        errorMessage +=
                            String.Format(
                                "An error occured in the UpdateKeyValueToMemberShipDatabase method because the core connection string was empty. See detailed errormessage. ConnectionString: {0}.",
                                connectionString);
                    }
                }
            }
            return -2;
        }
    }
}