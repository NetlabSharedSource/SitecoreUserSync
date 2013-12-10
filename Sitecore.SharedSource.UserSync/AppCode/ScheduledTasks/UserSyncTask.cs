using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.UserSync.AppCode.Log;
using Sitecore.SharedSource.UserSync.AppCode.Managers;
using Sitecore.SharedSource.UserSync.Managers;
using Sitecore.Tasks;

namespace Sitecore.SharedSource.UserSync.ScheduledTasks
{
    public class UserSyncTask
    {
        private const string Identifier = "UserSyncTask.RunJob";

        public void RunJob(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            try
            {
                if (scheduledItem != null)
                {
                    var itemIds = scheduledItem["Items"];
                    if (!String.IsNullOrEmpty(itemIds))
                    {
                        var idList = itemIds.Split('|');
                        if (idList.Any())
                        {
                            foreach (var id in idList)
                            {
                                if (ID.IsID(id))
                                {
                                    var userSyncItem = scheduledItem.Database.GetItem(new ID(id));
                                    try
                                    {
                                        if (userSyncItem != null)
                                        {
                                            var startedAt = DateTime.Now.ToLongDateString();
                                            Logging logBuilder = new Logging();
                                            var userSyncManager = new UserSyncManager();
                                            userSyncManager.RunUserSyncJob(userSyncItem, ref logBuilder);
                                            var finishededAt = DateTime.Now.ToLongDateString();
                                            if (logBuilder != null)
                                            {
                                                try
                                                {
                                                    MailManager.SendLogReport(ref logBuilder,
                                                                              GetUserSyncIdentifier(userSyncItem),
                                                                              userSyncItem);
                                                }
                                                catch (Exception exception)
                                                {
                                                    Diagnostics.Log.Error(
                                                        GetIdentifierText(userSyncItem, startedAt, finishededAt) +
                                                        " failed in sending out the mail. Please see the exception message for more details. Exception:" + exception.Message + ". Status:\r\n" +
                                                        logBuilder.GetStatusText(), typeof(UserSyncTask));
                                                }
                                                if (logBuilder.LogBuilder != null)
                                                {
                                                    if (!String.IsNullOrEmpty(logBuilder.LogBuilder.ToString()))
                                                    {
                                                        Diagnostics.Log.Error(
                                                            GetIdentifierText(userSyncItem, startedAt, finishededAt) +
                                                            " failed. " +
                                                            logBuilder.LogBuilder + "\r\nStatus:\r\n" +
                                                            logBuilder.GetStatusText(),
                                                            typeof(UserSyncTask));
                                                    }
                                                    else
                                                    {
                                                        Diagnostics.Log.Debug(
                                                            GetIdentifierText(userSyncItem, startedAt, finishededAt) +
                                                            " completed with success.\r\nStatus:\r\n" +
                                                            logBuilder.GetStatusText(),
                                                            typeof(UserSyncTask));
                                                    }
                                                }
                                                else
                                                {
                                                    Diagnostics.Log.Error(
                                                           GetIdentifierText(userSyncItem, startedAt, finishededAt) +
                                                           " failed. The Logging.LogBuilder object was null. " +
                                                           logBuilder + "\r\nStatus:\r\n" +
                                                           logBuilder.GetStatusText(),
                                                           typeof(UserSyncTask));
                                                }
                                            }
                                            else
                                            {
                                                Diagnostics.Log.Error(
                                                    GetIdentifierText(userSyncItem, startedAt, finishededAt) +
                                                    " - The Log object was null. This should not happen.",
                                                    typeof(UserSyncTask));
                                            }
                                        }
                                        else
                                        {
                                            Diagnostics.Log.Error(
                                                " - The Task item had Items defined in Items[] that was null. This should not happen.",
                                                typeof(UserSyncTask));
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        var itemId = userSyncItem != null ? userSyncItem.ID.ToString() : string.Empty;
                                        Diagnostics.Log.Error(
                                            Identifier +
                                            String.Format(
                                                " - An exception occured in the execution of the task in the foreach (Item UserSyncItem in itemArray) of the UserSync item: {0}. This UserSync job wasn't completed. Exception: {1}",
                                                itemId, exception.Message), typeof(UserSyncTask));
                                    }
                                }
                                else
                                {
                                    Diagnostics.Log.Error(
                                    Identifier +
                                    " - The provided value wasn't a correct Sitecore id. Please add at least one id to 'Items' field of the ScheduledItem. You can also use | to seperate ids. Therefore nothing was done.",
                                    typeof(UserSyncTask));
                                }
                            }
                        }
                        else
                        {
                            Diagnostics.Log.Error(
                                Identifier +
                                " - There wasn't defined any UserSync items to run. Please add at least one id to 'Items' field of the ScheduledItem. You can also use | to seperate ids. Therefore nothing was done.",
                                typeof(UserSyncTask));
                        }
                    }
                    else
                    {
                        Diagnostics.Log.Error(
                            Identifier + " - There wasn't defined any UserSync items to run. Therefore nothing was done.",
                            typeof(UserSyncTask));
                    }
                }
                else
                {
                    Diagnostics.Log.Error(
                            Identifier + " - The ScheduledItem was null. Therefore nothing was done.",
                            typeof(UserSyncTask));
                }
            }
            catch (Exception exception)
            {
                Diagnostics.Log.Error(Identifier + " - An exception occured in the execution of the task.", exception);
            }
        }

        private string GetIdentifierText(Item userSyncItem, string startedAt, string finishedAt)
        {
            return GetUserSyncIdentifier(userSyncItem) + " started " + startedAt + " and finished " + finishedAt;
        }

        private string GetUserSyncIdentifier(Item userSyncItem)
        {
            if (userSyncItem != null)
            {
                return userSyncItem.Name;
            }
            return String.Empty;
        }
    }
}