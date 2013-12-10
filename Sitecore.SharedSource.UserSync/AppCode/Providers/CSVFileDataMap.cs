using System;
using System.Web;
using System.IO;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.SharedSource.UserSync.AppCode.Log;

namespace Sitecore.SharedSource.UserSync.Providers
{
    public class CSVFileDataMap : CSVDataMap
    {
        public CSVFileDataMap(Database db, Item importItem, Logging logging) : base(db, importItem, logging)
        {
            Data = CsvFileData;
        }

        private string CsvFileData
        {
            get
            {
                try
                {
                    var datasource = DataSourceString;
                    if (!File.Exists(datasource))
                    {
                        datasource = HttpContext.Current != null ? HttpContext.Current.Server.MapPath(datasource) : Path.GetFullPath(datasource);
                        if (!File.Exists(datasource))
                        {
                            LogBuilder.Log("Error",
                                           String.Format(
                                               "The file defined in 'DataSource' field could not be found. DataSource: {0}.",
                                               datasource));
                            return String.Empty;
                        }
                    }
                    using (var streamreader = new StreamReader(datasource))
                    {
                        var fileStream = streamreader.ReadToEnd();
                        return fileStream;
                    }
                }
                catch (Exception ex)
                {
                    LogBuilder.Log("Error",
                                   String.Format("Reading the file failed with an exception. Exception: {0}.",
                                                 GetExceptionDebugInfo(ex)));
                }
                return String.Empty;
            }
        }
    }
}