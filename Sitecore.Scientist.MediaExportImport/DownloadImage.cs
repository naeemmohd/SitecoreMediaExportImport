using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Scientist.MediaExportImport
{
    public abstract class DownloadImageToMediaLibraryProcessor
    {
        public void Process(DownloadImageToMediaLibraryArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!CanProcess(args))
            {
                AbortPipeline(args);
                return;
            }

            Execute(args);
        }

        protected abstract bool CanProcess(DownloadImageToMediaLibraryArgs args);

        protected virtual void AbortPipeline(DownloadImageToMediaLibraryArgs args)
        {
            args.AbortPipeline();
        }

        protected abstract void Execute(DownloadImageToMediaLibraryArgs args);
    }
    public class DownloadImage : DownloadImageToMediaLibraryProcessor
    {
        protected override bool CanProcess(DownloadImageToMediaLibraryArgs args)
        {
            return !string.IsNullOrWhiteSpace(args.ImageUrl)
                && !string.IsNullOrWhiteSpace(args.ImageFilePath);
        }

        protected override void Execute(DownloadImageToMediaLibraryArgs args)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(args.ImageUrl, args.ImageFilePath);
            }
        }
    }
}
