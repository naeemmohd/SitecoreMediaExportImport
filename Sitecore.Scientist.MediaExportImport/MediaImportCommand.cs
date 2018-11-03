using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Security;
using System.Web;

namespace Sitecore.Scientist.MediaExportImport
{
    public class MediaImportCommand : Command
    {
        public MediaImportCommand()
        {
        }
        public override void Execute(CommandContext context)
        {
            if ((int)context.Items.Length == 1)
            {
                Item items = context.Items[0];
                NameValueCollection nameValueCollection = new NameValueCollection();
                nameValueCollection["uri"] = items.Uri.ToString();
                Context.ClientPage.Start(this, "Run", nameValueCollection);
            }
        }

        private bool extractRecursiveParam(string[] paramsArray)
        {
            bool flag;
            if ((int)paramsArray.Length <= 1)
            {
                flag = true;
            }
            else if (!bool.TryParse(paramsArray[1], out flag))
            {
                flag = true;
            }
            return flag;
        }

        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");
            if ((int)context.Items.Length != 1)
            {
                return CommandState.Hidden;
            }
            if (context.Items[0] == null)
            {
                return CommandState.Hidden;
            }
            return base.QueryState(context);
        }

        protected virtual void Run(ClientPipelineArgs args)
        {
            ItemUri itemUri = ItemUri.Parse(args.Parameters["uri"]);
            Item item = Database.GetItem(itemUri);
            Error.AssertItemFound(item);
            bool flag = true;
            string str1 = string.Concat(Settings.DataFolder.TrimStart(new char[] { '/' }), "\\", Settings.GetSetting("Sitecore.Scientist.MediaExportImport.ExportFolderName", "MediaExports")) + item.Paths.FullPath;
            if (!IsValidPath(str1))
            {
                str1 = HttpContext.Current.Server.MapPath("~/") + str1;
            }
            str1 = str1.Replace("/", "\\");
            if (FileUtil.FolderExists(FileUtil.MapPath(str1)))
            {
                Log.Info(string.Concat("Starting import media items from: ", string.Concat(Settings.DataFolder.TrimStart(new char[] { '/' }), "\\", Settings.GetSetting("Sitecore.Scientist.MediaExportImport.ExportFolderName", "MediaExports"))), this);
                ProgressBoxMethod progressBoxMethod = new ProgressBoxMethod(StartProcess);
                object[] objArray = new object[] { item, str1, flag };
                ProgressBox.Execute("Import Media Items...", "Import Media Items", progressBoxMethod, objArray);
            }
        }
        public bool IsValidPath(string path)
        {
            string result;
            return TryGetFullPath(path, out result);
        }
        public bool TryGetFullPath(string path, out string result)
        {
            result = String.Empty;
            if (String.IsNullOrWhiteSpace(path)) { return false; }
            bool status = false;

            try
            {
                result = Path.GetFullPath(path);
                status = !result.StartsWith("c:\\windows\\system32\\inetsrv\\");
            }
            catch (ArgumentException) { }
            catch (SecurityException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }

            return status;
        }
        public void StartProcess(params object[] parameters)
        {
            Item item = (Item)parameters[0];
            string path = (string)parameters[1];
            bool flag = (bool)parameters[2];
            MediaImporter mediaImporter = new MediaImporter(path);
            string[] files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                mediaImporter.ProcessMediaItems(item, file, flag);
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (var folder in folders)
            {
                mediaImporter.ProcessMediaFolder(item, folder, flag);
            }
            foreach (Item child in item.GetChildren())
            {
                Context.Job.Status.Messages.Add(string.Concat("Cleaning: ", child.Paths.ContentPath));
                mediaImporter.CleanMediaItems(child, flag, path);
            }
        }

    }
}
