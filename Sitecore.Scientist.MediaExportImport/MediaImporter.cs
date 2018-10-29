using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using System;
using System.IO;

namespace Sitecore.Scientist.MediaExportImport
{
    public class MediaImporter
    {
        public string Path
        {
            get;
            private set;
        }

        public MediaImporter(string path)
        {
            this.Path = path;
        }
        public void CleanMediaItems(Item rootMediaItem, bool recursive, string path)
        {
            var mediaItem = new MediaItem(rootMediaItem);
            if (rootMediaItem.TemplateID != TemplateIDs.MediaFolder && rootMediaItem.TemplateID != TemplateIDs.Node && rootMediaItem.TemplateID != TemplateIDs.MainSection)
            {
                string outputfile = path + "\\" + mediaItem.Name + "." + mediaItem.Extension;
                string[] files = Directory.GetFiles(path, mediaItem.Name + ".*", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    using (new Sitecore.SecurityModel.SecurityDisabler())
                    {
                        rootMediaItem.Delete();
                    }
                }
            }
            else if (rootMediaItem.TemplateID == TemplateIDs.MediaFolder || rootMediaItem.TemplateID == TemplateIDs.Node)
            {
                var outputFolder = string.Empty;

                string[] folders = Directory.GetDirectories(path, mediaItem.Name, SearchOption.TopDirectoryOnly);
                if (folders.Length == 0)
                {
                    using (new Sitecore.SecurityModel.SecurityDisabler())
                    {
                        rootMediaItem.Delete();
                    }
                }
                foreach (Item child in rootMediaItem.GetChildren())
                {
                    Context.Job.Status.Messages.Add(string.Concat("Cleaning: ", child.Paths.ContentPath));
                    this.CleanMediaItems(child, true, path + "\\" + rootMediaItem.Name);
                }
                return;
            }
        }
        public void ProcessMediaItems(Item rootMediaItem, string sourcePath, bool recursive, string path = null)
        {
            var mediaItemName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
            CreateorUpdateMedia(rootMediaItem.Paths.FullPath, sourcePath, mediaItemName);
            string str = string.Concat("Processed: ", Context.Job.Status.Processed, " items");
            Log.Info(str, this);
            JobStatus status = Context.Job.Status;
            status.Processed = status.Processed + (long)1;
            Context.Job.Status.Messages.Add(str);

        }
        public void ProcessMediaFolder(Item rootMediaItem, string sourcePath, bool recursive, string path = null)
        {
            var mediaItemName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
            Database masterDb = Factory.GetDatabase("master");
            Item myItem = masterDb.GetItem(rootMediaItem.Paths.FullPath + "/" + mediaItemName);
            if (myItem == null)
            {
                myItem = AddFolder(mediaItemName, rootMediaItem);
            }
            if (myItem != null && (myItem.TemplateID == TemplateIDs.MediaFolder || myItem.TemplateID == TemplateIDs.Node))
            {
                string[] files = Directory.GetFiles(sourcePath);
                foreach (var file in files)
                {
                    string processing = string.Concat("Processing: ", file.Replace("\\", "/"));
                    Log.Info(processing, this);
                    Context.Job.Status.Messages.Add(processing);
                    this.ProcessMediaItems(myItem, file, true);
                }
                string str = string.Concat("Processed: ", Context.Job.Status.Processed, " items");
                Log.Info(str, this);
                JobStatus status = Context.Job.Status;
                status.Processed = status.Processed + (long)1;
                Context.Job.Status.Messages.Add(str);
                string[] folders = Directory.GetDirectories(sourcePath);
                foreach (var folder in folders)
                {
                    string processing = string.Concat("Processing Folder: ", folder);
                    Log.Info(processing, this);
                    Context.Job.Status.Messages.Add(processing);
                    this.ProcessMediaFolder(myItem, folder, true);
                }
            }

        }
        public Item AddFolder(String name, Item Parent)
        {
            Database masterDb = Sitecore.Configuration.Factory.GetDatabase("master");
            if (Parent == null)
            {
                return null;
            }
            Item mediaFolder = null;
            try
            {
                Sitecore.Data.Items.TemplateItem FolderTemplate = masterDb.GetTemplate(TemplateIDs.MediaFolder);
                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
                    mediaFolder = Parent.Add(name, FolderTemplate);
                }
                string str = string.Concat("Processed ", Context.Job.Status.Processed, " items");
                Log.Info(str, this);
                JobStatus status = Context.Job.Status;
                status.Processed = status.Processed + (long)1;
                Context.Job.Status.Messages.Add(str);
            }
            catch (Exception ex)
            {
                return null;
            }
            return mediaFolder;
        }
        public Item CreateorUpdateMedia(string sitecorePath, string sourcePath, string mediaItemName)
        {
            try
            {
                Database masterDb = Sitecore.Configuration.Factory.GetDatabase("master");
                // ItemUri itemUri = ItemUri.Parse(sitecorePath + "/" + mediaItemName);
                // Item myItem = Database.GetItem(itemUri);
                // Create the options
                Sitecore.Resources.Media.MediaCreatorOptions options = new Sitecore.Resources.Media.MediaCreatorOptions();
                // Store the file in the database, not as a file
                options.FileBased = false;
                // Remove file extension from item name
                options.IncludeExtensionInItemName = false;
                // Overwrite any existing file with the same name
                options.OverwriteExisting = true;
                // Do not make a versioned template
                options.Versioned = false;
                // set the path
                options.Destination = sitecorePath;
                options.Destination = options.Destination + "/" + mediaItemName;
                options.Database = masterDb;
                // Now create the file
                Sitecore.Resources.Media.MediaCreator creator = new Sitecore.Resources.Media.MediaCreator();
                var mediaItem = masterDb.GetItem(sourcePath);
                if (mediaItem != null)
                {
                    FileInfo fi = new System.IO.FileInfo(sourcePath);
                    FileStream fs = fi.OpenRead();
                    MemoryStream ms = new MemoryStream();
                    ms.SetLength(fs.Length);
                    fs.Read(ms.GetBuffer(), 0, (int)fs.Length);
                    ms.Flush();
                    fs.Close();
                    MediaItem updatedItem = creator.AttachStreamToMediaItem(ms, sitecorePath, mediaItemName, options);
                    if (updatedItem != null)
                    {
                        ms.Dispose();
                        return updatedItem;
                    }
                }
                else
                {
                    MediaItem newItem = creator.CreateFromFile(sourcePath, options);
                    if (newItem != null)
                    {
                        return newItem;
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return null;
        }
        private bool IsDirectory(string path)
        {

            System.IO.FileAttributes fa = System.IO.File.GetAttributes(path);

            return (fa & FileAttributes.Directory) != 0;
        }
    }
}
