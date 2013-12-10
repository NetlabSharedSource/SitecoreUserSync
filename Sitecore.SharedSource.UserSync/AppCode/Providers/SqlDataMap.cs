using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.AppCode.Log;
using Sitecore.SharedSource.UserSync.Mappings.Fields;
using Sitecore.SharedSource.UserSync.Extensions;
using System.Collections;

namespace Sitecore.SharedSource.UserSync.Providers
{
	public class SqlDataMap : BaseDataMap {
		
		#region Properties

		#endregion Properties

		#region Constructor

        public SqlDataMap(Database db, Item importItem, Logging logging)
            : base(db, importItem, logging)
        {
            if (string.IsNullOrEmpty(Query))
            {
                LogBuilder.Log("Error", "the 'Query' field was not set");
            }
		}
		
		#endregion Constructor

        #region Override Methods

        /// <summary>
        /// uses a SqlConnection to get data
        /// </summary>
        /// <returns></returns>
        public override IList<object> GetImportData()
        {
            DataSet ds = new DataSet();
            SqlConnection dbCon = new SqlConnection(this.DataSourceString);
            dbCon.Open();

            SqlDataAdapter adapter = new SqlDataAdapter(this.Query, dbCon);
            adapter.Fill(ds);
            dbCon.Close();

            DataTable dt = ds.Tables[0].Copy();
            
            var result = (from DataRow dr in dt.Rows
                    select dr);
            IList<object> list = new List<object>();
            foreach(var o in result)
            {
                list.Add(o);
            }
            return list;
        }

        /// <summary>
        /// doesn't handle any custom data
        /// </summary>
        /// <param name="newUser"></param>
        /// <param name="importRow"></param>
        public override bool ProcessCustomData(ref User newUser, object importRow, out bool processedCustomData)
        {
            processedCustomData = false;
            return true;
        }
        
        /// <summary>
        /// gets custom data from a DataRow
        /// </summary>
        /// <param name="importRow"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override string GetFieldValue(object importRow, string fieldName, ref string errorMessage)
        {
            try
            {
                DataRow dataRow = importRow as DataRow;
                if (dataRow != null)
                {
                    if (!String.IsNullOrEmpty(fieldName))
                    {
                        object fieldValue = dataRow[fieldName];
                        return (fieldValue != null) ? fieldValue.ToString() : String.Empty;
                    }
                    else
                    {
                        errorMessage += String.Format("The GetFieldValue method failed because the the 'fieldName' was null or empty. ImportRow: {0}.", GetImportRowDebugInfo(importRow));
                    }
                }
                else
                {
                    errorMessage += String.Format("The GetFieldValue method failed because the Import Row was null. FieldName: {0}.", fieldName);
                }
            }
            catch (Exception ex)
            {
                errorMessage += String.Format("The GetFieldValue method failed with an exception. ImportRow: {0}. FieldName: {1}. Exception: {2}.", GetImportRowDebugInfo(importRow), fieldName, ex);
            }
            return String.Empty;
        }

	    public override string GetImportRowDebugInfo(object importRow)
	    {
	        string errorMessage = String.Empty;
	        string debugInfo = GetKeyValueFromImportRow(importRow, ref errorMessage);
	        if (!String.IsNullOrEmpty(errorMessage))
	        {
                LogBuilder.Log("Error", String.Format("In the GetValueFromFieldToIdentifyTheSameItemsBy method failed: {0}. DebugInfo: {1}", errorMessage, debugInfo));
	            return debugInfo;
	        }
	        DataRow dataRow = importRow as DataRow;
	        if (dataRow != null)
	        {
                for(int i=0; i<dataRow.ItemArray.Count(); i++)
                {
                    var column = dataRow.ItemArray[i];
                    if (!String.IsNullOrEmpty(column + ""))
                    {
                        if (column is int)
                        {
                            debugInfo += column;
                        }
                        else if (column is string)
                        {
                            debugInfo += column;
                        }
                        else if (column is DateTime)
                        {
                            debugInfo += column;
                        }
                        if (i != dataRow.ItemArray.Count() - 1)
                        {
                            debugInfo += ", ";
                        }
                    }
                }
	        }
	        return debugInfo;
	    }

	    #endregion Override Methods

        #region Methods

        #endregion Methods
    }
}
