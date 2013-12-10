﻿using Sitecore.Shell.Framework.Commands;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using System.Collections.Specialized;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;

namespace Sitecore.SharedSource.UserSync.Shell.Commands
{
    [Serializable]
    public class UserSyncWizardCommand: Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Error.AssertObject(context, "context");
            if (context.Items.Length == 1)
            {
                Item item = context.Items[0];
                var parameters = new NameValueCollection();
                parameters["id"] = item.ID.ToString();
                parameters["language"] = item.Language.ToString();
                parameters["version"] = item.Version.ToString();
                parameters["database"] = item.Database.Name;
                Context.ClientPage.Start(this, "Run", parameters);
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            if (context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }
            Item item = context.Items[0];
            if (!base.HasField(item, FieldIDs.LayoutField))
            {
                return CommandState.Hidden;
            }
            return base.QueryState(context);
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                UrlString str2 = new UrlString(UIUtil.GetUri("control:UserSyncWizard"));
                str2.Append("id", args.Parameters["id"]);
                str2.Append("la", args.Parameters["language"]);
                str2.Append("vs", args.Parameters["version"]);
                SheerResponse.ShowModalDialog(str2.ToString(), "250", "300");
                args.WaitForPostBack();
            }
        }
    }
}