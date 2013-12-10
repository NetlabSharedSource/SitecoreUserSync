using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BackPack.Modules.AppCode.Import.Utility;
using Sitecore.Data.Items;
using Sitecore.Data;
using System.Data;
using System.Data.SqlClient;
using Sitecore.Security.Accounts;
using Sitecore.SharedSource.UserSync.AppCode.Log;

namespace Sitecore.SharedSource.UserSync.Providers
{
	public class XmlDataMap : BaseDataMap {

		#region Properties

	    protected readonly int DebugImportRowXmlCharacterLength = 50;

	    #endregion Properties

		#region Constructor

        public XmlDataMap(Database db, Item importItem, Logging logging)
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
	    /// doesn't handle any custom data
	    /// </summary>
	    /// <param name="newItem"></param>
	    /// <param name="importRow"></param>
	    /// <param name="processedCustomData"></param>
	    public override bool ProcessCustomData(ref User user, object importRow, out bool processedCustomData)
        {
            processedCustomData = false;
            return true;
        }

        public override IList<object> GetImportData()
	    {
            throw new NotImplementedException();
	    }

	    /// <summary>
	    /// gets custom data from a DataRow
	    /// </summary>
	    /// <param name="importRow"></param>
	    /// <param name="fieldName"></param>
	    /// <param name="errorMessage"></param>
	    /// <returns></returns>
	    public override string GetFieldValue(object importRow, string fieldName, ref string errorMessage)
	    {
            try
            {
	            var xElement = importRow as XElement;
                if (xElement != null)
	            {
                    if (!String.IsNullOrEmpty(fieldName))
                    {
                        try
                        {
                            // First retrieve the fieldName as an attribute
                            var attribute = xElement.Attribute(fieldName);
                            if (attribute != null)
                            {
                                string value = attribute.Value;
                                if (!String.IsNullOrEmpty(value))
                                {
                                    return value;
                                }
                            }
                            else
                            {
                                // Then retrieve the fieldname as an subelement
                                var subElements = xElement.Elements(fieldName);
                                var elementsList = subElements.ToList();
                                if (elementsList.Count() > 1)
                                {
                                    // Log eror since document format is wrong. Has two or more elements with same name.
                                    errorMessage +=
                                        String.Format(
                                            "The GetFieldValue method failed because the fieldName '{0}' resulted in more than one subelement in the Import Row. FieldName: {0}. ImportRow: {1}.",
                                            fieldName, GetImportRowDebugInfo(importRow));
                                }
                                else if (elementsList.Count() == 1)
                                {
                                    var subElement = elementsList.First();
                                    if (subElement != null)
                                    {
                                        var value = subElement.Value;
                                        if (!String.IsNullOrEmpty(value))
                                        {
                                            return value;
                                        }
                                    }
                                }
                            }
                        }
                        catch (XmlException)
                        {
                            // We do nothing since this is most likely because we have a xpath query as the fieldname.
                        }

                        // Now finally try to retrieve through a xPath query
                        var result = ExecuteXPathQueryOnXElement(xElement, fieldName, ref errorMessage);
                        if (!String.IsNullOrEmpty(errorMessage))
                        {
                            errorMessage += String.Format("The GetFieldValue method failed in executing the ExecuteXPathQueryOnXElement method. ErrorMessage: {0}.", errorMessage);
                        }
                        string fieldValue;
                        var enumerable = result as IList<object> ?? result.Cast<object>().ToList();
                        if (TryParseAttribute(enumerable, out fieldValue, ref errorMessage))
                        {
                            return fieldValue;
                        }
                        if (TryParseElement(enumerable, out fieldValue, ref errorMessage))
                        {
                            return fieldValue;
                        }
                    }
                    else
                    {
                        errorMessage += String.Format("The GetFieldValue method failed because the 'fieldName' was null or empty. FieldName: {0}. ImportRow: {1}.", fieldName, GetImportRowDebugInfo(importRow));
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
        
        protected IEnumerable<XElement> ExecuteXPathQueryOnXElement(XElement xElement, ref string errorMessage)
	    {
	        return ExecuteXPathQueryOnXElement(xElement, Query, ref errorMessage);
	    }

        private bool TryParseAttribute(IEnumerable result, out string fieldValue, ref string errorMessage)
        {
            fieldValue = String.Empty;
            try
            {
                var xAttributes = result.Cast<XAttribute>();
                var attributes = xAttributes as IList<XAttribute> ?? xAttributes.ToList();
                if (attributes.Count() > 1)
                {
                    errorMessage +=
                        String.Format(
                            "The GetFieldValue method failed because the helper method TryParseAttribute found more than one attribute with the same name ExecuteXPathQueryOnXElement method.");
                }
                else if (attributes.Count() == 1)
                {
                    var xAttribute = attributes.First();
                    if (xAttribute != null)
                    {
                        fieldValue = xAttribute.Value;
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                return false;
            }
            return false;
        }

        private bool TryParseElement(IEnumerable result, out string fieldValue, ref string errorMessage)
        {
            fieldValue = String.Empty;
            try
            {
                var xElements = result.Cast<XElement>();
                var elements = xElements as IList<XElement> ?? xElements.ToList();
                if (elements.Count() > 1)
                {
                    errorMessage +=
                        String.Format(
                            "The GetFieldValue method failed because the helper method TryParseElement found more than one element with the same name ExecuteXPathQueryOnXElement method.");
                }
                else if (elements.Count() == 1)
                {
                    var xElement = elements.First();
                    if (xElement != null)
                    {
                        fieldValue = xElement.Value;
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                return false;
            }
            return false;
        }

        protected IEnumerable<XElement> ExecuteXPathQueryOnXElement(XElement xElement, string query, ref string errorMessage)
        {
            if (xElement != null)
            {
                try
                {
                    return xElement.XPathSelectElements(query);
                }
                catch (Exception ex)
                {
                    errorMessage += String.Format("An exception occured in the ExecuteXPathQueryOnXElement method executing the XPath query. Query: {0}. Exception: {1}.", query, GetExceptionDebugInfo(ex));
                }
            }
            errorMessage += "In ExecuteXPathQueryOnXElement method the XDocument was null.";
            return null;
        }

        protected IList<object> ExecuteXPathQuery(XDocument xDocument)
        {
            if (xDocument != null)
            {
                var elements = xDocument.XPathSelectElements(Query);
                IList<object> list = new List<object>();
                foreach (var element in elements)
                {
                    list.Add(element);
                }
                return list;
            }
            LogBuilder.Log("Error", "In ExecuteXPathQuery method the XDocument was null.");
            return null;
        }


	    public override string GetImportRowDebugInfo(object importRow)
	    {
	        if (importRow != null)
	        {
                string errorMessage = String.Empty;
                var keyValue = GetKeyValueFromImportRow(importRow, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error", String.Format("In the GetImportRowDebugInfo method failed: {0}.", errorMessage));
                    return keyValue;
                }

                if (!string.IsNullOrEmpty(keyValue))
	            {
                    return keyValue;
	            }
	            if (importRow is XElement)
	            {
	                var xElement = (XElement) importRow;
	                var innerXml = xElement.GetInnerXml();
	                if (!string.IsNullOrEmpty(innerXml))
	                {
	                    if (innerXml.Length > DebugImportRowXmlCharacterLength)
	                    {
	                        return innerXml.Substring(0, DebugImportRowXmlCharacterLength);
	                    }
	                    return innerXml;
	                }
	            }
	            return importRow.ToString();
	        }
	        return String.Empty;
	    }

	    #endregion Override Methods

        #region Methods

        #endregion Methods
    }
}
