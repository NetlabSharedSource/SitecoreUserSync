using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Sitecore.Security.Accounts;
using System.Data;

namespace Sitecore.SharedSource.UserSync.AppCode.Utility
{
    public static class UserKeyUtil
    {
        private const string CommandPatternGetUserName = "SELECT [UserName] FROM aspnet_Users WHERE ([MobileAlias]='{0}')";
        private const string CommandPatternGetMobileAlias = "SELECT [MobileAlias] FROM aspnet_Users WHERE ([UserName]='{0}')";
        private const string CommandPatternUpdateMobileAlias = "UPDATE aspnet_Users SET MobileAlias = '{0}' WHERE (UserName = '{1}')";

        public static List<User> GetUsersFromKey(string keyValue, ref string errorMessage)
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
                        string commandString = String.Format(CommandPatternGetUserName, keyValue);
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
            return null;
        }

        public static string GetKeyValueFromUser(User user, ref string errorMessage)
        {
            string connectionString = Configuration.Settings.GetConnectionString("core");
            if (!String.IsNullOrEmpty(connectionString))
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string commandString = String.Format(CommandPatternGetMobileAlias, user.Profile.UserName);
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

        public static int UpdateKeyValueToMemberShipDatabase(string keyValue, string userName, ref string errorMessage)
        {
            var connectionString = Sitecore.Configuration.Settings.GetConnectionString("core");
            if (!String.IsNullOrEmpty(connectionString))
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var commandString = String.Format(CommandPatternUpdateMobileAlias, userName, keyValue);
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
            return -2;
        }

        public static bool ChangeUsername(string existingUsername, string newUsername, ref string errorMessage)
        {
            if (String.IsNullOrEmpty(existingUsername))
            {
                errorMessage += String.Format("The 'existingUsername' was null or empty in the ChangeUsername method. The username was not changed. " + 
                    "ExistingUsername: {0}. NewUsername: {1}.", existingUsername, newUsername);
                return false;
            }

            if (String.IsNullOrEmpty(newUsername))
            {
                errorMessage += String.Format("The 'newUsername' was null or empty in the ChangeUsername method. The username was not changed. " +
                    "ExistingUsername: {0}. NewUsername: {1}.", existingUsername, newUsername);
                return false;
            }

            // This test should have been performed earlier, but we still test it.
            if (existingUsername == newUsername)
            {
                return false;
            }

            if (User.Exists(newUsername))
            {
                errorMessage += String.Format("The 'newUsername' already exists in the membershipdatabase as an username on another user. Therefore the username change was aborted in the ChangeUsername method. " +
                    "ExistingUsername: {0}. NewUsername: {1}.", existingUsername, newUsername);
                return false;
            }

            var connectionString = Sitecore.Configuration.Settings.GetConnectionString("core");
            if (!String.IsNullOrEmpty(connectionString))
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        using (SqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "UPDATE aspnet_Users SET UserName=@NewUsername, LoweredUserName=@LoweredNewUsername WHERE UserName=@OldUsername";

                            SqlParameter parameter = new SqlParameter("@OldUsername", SqlDbType.VarChar);
                            parameter.Value = existingUsername;
                            command.Parameters.Add(parameter);

                            parameter = new SqlParameter("@NewUsername", SqlDbType.VarChar);
                            parameter.Value = newUsername;
                            command.Parameters.Add(parameter);

                            parameter = new SqlParameter("@LoweredNewUsername", SqlDbType.VarChar);
                            parameter.Value = newUsername.ToLower();
                            command.Parameters.Add(parameter);

                            int result = command.ExecuteNonQuery();
                            if (result > 1)
                            {
                                errorMessage +=
                                String.Format(
                                    "The ChangeUsername method failed because more than one row was updated with the username. Since the username should be unique, this is an error. " +
                                    "RowsAffected: {0}. existingUsername: {1}. newUsername: {2}. connectionString: {3}.",
                                    result, existingUsername, newUsername, connectionString);
                                return false;
                            }
                            else if (result == 0)
                            {
                                errorMessage += String.Format("The ChangeUsername method failed because none rows was updated with the username. This is an error. " +
                                    "RowsAffected: {0}. existingUsername: {1}. newUsername: {2}. connectionString: {3}.",
                                    result, existingUsername, newUsername, connectionString);
                                return false;
                            }
                            else if (result < 0)
                            {
                                errorMessage += String.Format("The ChangeUsername method failed because an error occured. This is an error. " +
                                    "RowsAffected: {0}. existingUsername: {1}. newUsername: {2}. connectionString: {3}.",
                                    result, existingUsername, newUsername, connectionString);
                                return false;
                            }
                            else
                            {
                                // result == 1
                                return true;
                            } 
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMessage +=
                        String.Format(
                            "An error occured in the ChangeUsername method. See detailed errormessage. ExistingUsername: {0}. NewUsername: {1}. ConnectionString: {2}. Exception: {3}.",
                            existingUsername, newUsername, connectionString, ex);
                }
            }
            else
            {
                errorMessage += String.Format("The connectionstring for the core database was null or empty in the ChangeUsername method.");                
            }
            return false;
        }
    }
}