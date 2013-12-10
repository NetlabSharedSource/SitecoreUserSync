using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Sitecore.Data.Items;

namespace BackPack.Modules.AppCode.Import.Utility
{
    public static class LogUtil
    {
        public static string ScItemsDebugInfo(IEnumerable<Item> items)
        {
            var result = string.Empty;
            if (items != null)
                foreach (var item in items)
                {
                    if (result.Length > 0)
                    {
                        result += "|";
                    }
                    if (item == null)
                    {
                        result += "[null]";
                    }
                    else
                    {
                        result += item.ID;
                    }
                }
            return result;
        }

        public static string ScItemsDebugInfo(Item item)
        {
            if (item != null)
            {
                return string.Format("'{0}|{1}'", item.Name, item.ID);
            }
            return "[null]";
        }

        public static string GetInnerXml(this XElement element)
        {
            var innerXml = new StringBuilder();

            foreach (XNode node in element.Nodes())
            {
                innerXml.Append(node);
            }
            return innerXml.ToString();
        }
    }
}