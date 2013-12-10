using System.Globalization;
using Sitecore.SharedSource.UserSync.AppCode.Log;
using Sitecore.SharedSource.UserSync.AppCode.Managers;
using Sitecore.SharedSource.UserSync.Managers;
using Sitecore.SharedSource.UserSync.Providers;
using Sitecore.SharedSource.UserSync.ScheduledTasks;
using Sitecore.Web.UI.Pages;
using Sitecore.Diagnostics;
using System;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Globalization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs;
using System.Web;

namespace Sitecore.SharedSource.UserSync.Shell.Wizards
{
    public class UserSyncWizard : WizardForm
    {
        // Fields
        private const string Failure = "Failure";
        private const string Success = "Success";
        private string TextJobHandle = "JobHandle";
        const string TextUserSyncDataValue = "UserSyncDataValue";
        
        protected Memo ErrorText;
        protected Literal ImportStatusText;
        protected Literal FailedStatusText;
        protected Border FailedText;
        protected Literal NotPresentStatusText;
        protected Border NotPresentText;
        protected Radiobutton IncrementalPublish;
        protected Border IncrementalPublishPane;
        protected Border Languages;
        protected Groupbox LanguagesPanel;
        protected Border NoPublishingTarget;
        protected Checkbox PublishChildren;
        protected Border PublishChildrenPane;
        protected Groupbox PublishingPanel;
        protected Border PublishingTargets;
        protected Groupbox PublishingTargetsPanel;
        protected Border PublishingText;
        protected Radiobutton Republish;
        protected Border ResultLabel;
        protected Memo ResultText;
        protected Scrollbox SettingsPane;
        protected Border ShowResultPane;
        protected Literal Status;
        protected Literal Welcome;
        protected Memo Data;

        private string JobHandle
        {
            get
            {
                return StringUtil.GetString(ServerProperties[TextJobHandle]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                ServerProperties[TextJobHandle] = value;
            }
        }
        
        // Events
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
            }
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");

            NextButton.Header = "Next >";
            if (page == "StartImport")
            {
                NextButton.Header = "Import >";
            }
            base.ActivePageChanged(page, oldPage);
            if (page == "Importing")
            {
                NextButton.Disabled = true;
                BackButton.Disabled = true;
                CancelButton.Disabled = true;
                SheerResponse.SetInnerHtml("PublishingText", Translate.Text("Importing..."));
                SheerResponse.SetInnerHtml("PublishingTarget", "&nbsp;");
                SheerResponse.Timer("StartImport", 10);
            }
        }

        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(newpage, "newpage");
            if (page == "Retry")
            {
                newpage = "StartImport";
            }

            if (newpage == "Importing")
            {

            }
            return base.ActivePageChanging(page, ref newpage);
        }

        protected void ShowResult()
        {
            this.ShowResultPane.Visible = false;
            this.ResultText.Visible = true;
            this.ResultLabel.Visible = true;
        }


        #region Helper methods

        private Item GetUserSyncItem()
        {
            string id = HttpContext.Current.Request.QueryString["id"];
            if (ID.IsID(id))
            {
                return Configuration.Factory.GetDatabase("master").GetItem(new ID(id));
            }
            return null;
        }

        private string GetStatusText(Item userSyncItem, Logging logBuilder, string startedAt, string finishedAt)
        {
            if (logBuilder != null)
            {
                if (logBuilder.LogBuilder != null)
                {
                    var logString = logBuilder.LogBuilder.ToString();
                    if (String.IsNullOrEmpty(logString))
                    {
                        logString += "The import completed successfully.\r\n\r\nStatus:\r\n" +
                                     logBuilder.GetStatusText();
                    }
                    else
                    {
                        logString = "The import failed.\r\n\r\nStatus:\r\n" + logBuilder.GetStatusText() + "\r\n\r\n" +
                                    logString;
                    }
                    return logString;
                }
                return GetIdentifierText(userSyncItem, startedAt, finishedAt) + " failed. The Logging.LogBuilder object was null. " + logBuilder + "\r\nStatus:\r\n" + logBuilder.GetStatusText();
            }
            return GetIdentifierText(userSyncItem, startedAt, finishedAt) + " - The Log object was null. This should not happen.";
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

        #endregion
        
        protected void StartImport()
        {
            var userSyncItem = GetUserSyncItem();

            var userSyncManager = new UserSyncManager();
            string errorMessage = String.Empty;
            var map = userSyncManager.InstantiateDataMap(userSyncItem, ref errorMessage);
            if (map != null)
            {
                var options = new JobOptions("UserSyncWizard", "Job category name", Context.Site.Name,
                                             new UserSyncWizard(), "Run", new object[] {map, map.LogBuilder});
                var job = JobManager.Start(options);
                job.Options.CustomData = map.LogBuilder;
                JobHandle = job.Handle.ToString();
                SheerResponse.Timer("CheckStatus", 5);
            }
            else
            {
                Active = "LastPage";
                BackButton.Disabled = true;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ResultText.Value = errorMessage;
                }
            }
        }

        protected void Run(BaseDataMap map, Logging logBuilder)
        {
            Context.Job.Status.State = JobState.Running;

            var startedAt = DateTime.Now.ToLongDateString();
            map.Process();
            var finishededAt = DateTime.Now.ToLongDateString();
            try
            {
                MailManager.SendLogReport(ref logBuilder,
                                          GetUserSyncIdentifier(map.ImportItem),
                                          map.ImportItem);
            }
            catch (Exception exception)
            {
                Diagnostics.Log.Error(
                    GetIdentifierText(map.ImportItem, startedAt, finishededAt) +
                    " failed in sending out the mail. Please see the exception message for more details. Exception:" + exception.Message + ". Status:\r\n" +
                    logBuilder.GetStatusText(), typeof(UserSyncTask));
            }
            var logString = logBuilder.LogBuilder.ToString();
            if (!String.IsNullOrEmpty(logString))
            {
                Context.Job.Status.Failed = true;
            }
            else
            {
                Context.Job.Status.State = JobState.Finished;    
            }   
        }

        public void CheckStatus()
        {
            var job = JobManager.GetJob(Handle.Parse(JobHandle));
            if (job != null)
            {
                var status = job.Status;
                var state = status.State;
                var logBuilder = (Logging)job.Options.CustomData;

                if (status == null)
                {
                    throw new Exception("The sync process was unexpectedly interrupted.");
                }
                if (state == JobState.Running)
                {
                    NextButton.Disabled = true;
                    BackButton.Disabled = false;
                    CancelButton.Disabled = false;
                    if (logBuilder.TotalNumberOfUsers > 0)
                    {
                        ImportStatusText.Text = logBuilder.ProcessedUsers + " of " + logBuilder.TotalNumberOfUsers + " users processed.";
                    }
                    else
                    {
                        ImportStatusText.Text = "Retrieving import data...";
                    }
                    if (logBuilder.ProcessedUsers == logBuilder.TotalNumberOfUsers && logBuilder.ProcessedUsers != 0)
                    {
                        NotPresentStatusText.Text = "Processing users not present in import...";

                        if (logBuilder.NotPresentInImportProcessedUsers > 0)
                        {
                            NotPresentStatusText.Text = logBuilder.NotPresentInImportProcessedUsers + " of " +
                                                        logBuilder.TotalNumberOfNotPresentInImportUsers +
                                                        " users not present in import processed.";
                        }
                    }
                    FailedText.Visible = logBuilder.FailureUsers > 0;
                    FailedStatusText.Text = logBuilder.FailureUsers + " users failed.";
                }
                else if (state == JobState.Finished)
                {
                    Status.Text = String.Format("Users processed: {0}.", logBuilder.ProcessedUsers.ToString(CultureInfo.CurrentCulture));
                    Active = "LastPage";
                    BackButton.Disabled = true;
                    string str2 = GetStatusText(GetUserSyncItem(), logBuilder, String.Empty, string.Empty);
                    if (!string.IsNullOrEmpty(str2))
                    {
                        ResultText.Value = str2;
                    }
                    return;
                }
                SheerResponse.Timer("CheckStatus", 5);
            }
        }
    }
}