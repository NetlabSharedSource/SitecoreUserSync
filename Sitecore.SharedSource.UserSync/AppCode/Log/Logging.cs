using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Sitecore.SharedSource.UserSync.AppCode.Log
{
    public class Logging
    {
        public int CreatedUsers { get; set; }

        public int NotPresentInImportProcessedUsers { get; set; }

        public int DeletedUsers { get; set; }
        
        public int UpdatedRolesUsers { get; set; }

        public int FailedDeletedUsers { get; set; }

        public int FailedNotPresentInImportProcessedUsers { get; set; }

        public int FailureUsers { get; set; }

        public int ProcessedUsers { get; set; }

        public int UpdatedFields { get; set; }

        public int SucceededUsers { get; set; }

        public int ProcessedCustomDataUsers { get; set; }

        public int TotalNumberOfUsers { get; set; }

        public int TotalNumberOfNotPresentInImportUsers { get; set; }

        public Logging()
        {
            LogBuilder = new StringBuilder();
            ProcessedUsers = 0;
            CreatedUsers = 0;
            NotPresentInImportProcessedUsers = 0;
            FailedNotPresentInImportProcessedUsers = 0;
            DeletedUsers = 0;
            UpdatedRolesUsers = 0;
            FailedDeletedUsers = 0;
            FailureUsers = 0;
            UpdatedFields = 0;
            SucceededUsers = 0;
            ProcessedCustomDataUsers = 0;
            TotalNumberOfUsers = 0;
            TotalNumberOfNotPresentInImportUsers = 0;
        }

        /// <summary>
        /// the log is returned with any messages indicating the status of the import
        /// </summary>
        protected StringBuilder logBuilder;

        public StringBuilder LogBuilder
        {
            get { return logBuilder; }
            set { logBuilder = value; }
        }

        public void Log(string errorType, string message)
        {
            string logText = String.Format("{0} : {1}", errorType, message);
            LogBuilder.AppendFormat("{0} : {1}", errorType, message).AppendLine().AppendLine();
            Diagnostics.Log.Error("UserSync --- " + logText, this);
        }

        //public void Log(string message)
        //{
        //    string logText = String.Format("{0}", message);
        //    LogBuilder.AppendFormat("{0}", message).AppendLine().AppendLine();
        //    Diagnostics.Log.Error(logText, this);
        //}

        public string GetStatusText()
        {
            var statusText = String.Empty;
            statusText += WriteLine("ProcessedUsers", ProcessedUsers);
            statusText += WriteLine("CreatedUsers", CreatedUsers);
            statusText += WriteLine("NotPresentInImportProcessedUsers", NotPresentInImportProcessedUsers);
            statusText += WriteLine("FailedNotPresentInImportProcessedUsers", FailedNotPresentInImportProcessedUsers);
            statusText += WriteLine("DeletedUsers", DeletedUsers);
            statusText += WriteLine("UpdatedRolesUsers", UpdatedRolesUsers);
            statusText += WriteLine("FailedDeletedUsers", FailedDeletedUsers);
            statusText += WriteLine("FailureUsers", FailureUsers);
            statusText += WriteLine("UpdatedFields", UpdatedFields);
            statusText += WriteLine("SucceededUsers", SucceededUsers);
            statusText += WriteLine("ProcessedCustomDataUsers", ProcessedCustomDataUsers);
            return statusText;
        }

        private string WriteLine(string type, int itemCount)
        {
            if (itemCount != 0)
            {
                return type + ": " + itemCount + "\r\n";
            }
            return String.Empty;
        }
    }
}
