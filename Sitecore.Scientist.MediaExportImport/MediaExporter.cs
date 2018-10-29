using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Jobs;
using Sitecore.Pipelines;
using Sitecore.Resources.Media;
using System.IO;

namespace Sitecore.Scientist.MediaExportImport
{
    public class DownloadImageToMediaLibraryArgs : PipelineArgs
    {
        public Database Database { get; set; }

        public string ImageFileName { get; set; }

        public string ImageFilePath { get; set; }

        public string ImageItemName { get; set; }

        public string ImageUrl { get; set; }

        public string MediaId { get; set; }

        public string MediaLibaryFolderPath { get; set; }

        public string MediaPath { get; set; }

        public bool FileBased { get; set; }

        public bool IncludeExtensionInItemName { get; set; }

        public bool OverwriteExisting { get; set; }

        public bool Versioned { get; set; }
    }
    public class MediaExporter
    {
        public string File
        {
            get;
            private set;
        }

        public MediaExporter(string file)
        {
            this.File = file;
        }
        public void CleanMediaFiles(Item rootMediaItem, string path)
        {
            var mediaItemName = System.IO.Path.GetFileNameWithoutExtension(path);
            Database masterDb = Factory.GetDatabase("master");
            Item myItem = masterDb.GetItem(rootMediaItem.Paths.FullPath + "/" + mediaItemName);
            if (myItem == null)
            {
                FileAttributes attr = System.IO.File.GetAttributes(path);
                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    System.IO.File.Delete(path);
                }
            }
            else
            {
                FileAttributes attr = System.IO.File.GetAttributes(path);
                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string[] files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        this.CleanMediaFiles(myItem, file);
                    }
                    string[] folders = Directory.GetDirectories(path);
                    foreach (var folder in folders)
                    {
                        this.CleanMediaFiles(myItem, folder);
                    }
                }
            }
        }
        protected virtual void DownloadImage(MediaItem mediaItem, string path = null)
        {
            var media = MediaManager.GetMedia(mediaItem);
            var stream = media.GetStream();
            var outputfile = string.Empty;
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(mediaItem.Extension))
            {
                outputfile = path + "\\" + mediaItem.Name + "." + mediaItem.Extension;
                string[] files = Directory.GetFiles(path, mediaItem.Name + ".*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    if (file != outputfile)
                        System.IO.File.Delete(file);
                }
            }
            else if (!string.IsNullOrEmpty(mediaItem.Extension))
            {
                outputfile = this.File + "\\" + mediaItem.Name + "." + mediaItem.Extension;
                string[] files = Directory.GetFiles(this.File, mediaItem.Name + ".*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    if (file != outputfile)
                        System.IO.File.Delete(file);
                }
            }
            else
            {
                return;
            }

            using (var targetStream = System.IO.File.OpenWrite(outputfile))
            {
                stream.CopyTo(targetStream);
                targetStream.Flush();
            }

        }

        public void ProcessMediaItems(Item rootMediaItem, bool recursive, string path = null)
        {
            if (rootMediaItem.TemplateID != TemplateIDs.MediaFolder && rootMediaItem.TemplateID != TemplateIDs.Node && rootMediaItem.TemplateID != TemplateIDs.MainSection)
            {
                string str = string.Concat("Processed ", Context.Job.Status.Processed, " items");
                Log.Info(str, this);
                JobStatus status = Context.Job.Status;
                status.Processed = status.Processed + (long)1;
                Context.Job.Status.Messages.Add(str);
                this.DownloadImage(new MediaItem(rootMediaItem), path);
                return;

            }
            else if (rootMediaItem.TemplateID == TemplateIDs.MediaFolder || rootMediaItem.TemplateID == TemplateIDs.Node)
            {
                var outputFolder = string.Empty;
                if (!string.IsNullOrEmpty(path))
                {
                    outputFolder = path + "\\" + rootMediaItem.Name;
                }
                else
                {
                    outputFolder = this.File + "\\" + rootMediaItem.Name;
                }
                FileUtil.CreateFolder(FileUtil.MapPath(outputFolder));

                Context.Job.Status.Messages.Add(string.Concat("Processing: ", rootMediaItem.Paths.ContentPath));
                foreach (Item child in rootMediaItem.GetChildren())
                {
                    this.ProcessMediaItems(child, true, outputFolder);
                }
                return;
            }
        }
    }
}
