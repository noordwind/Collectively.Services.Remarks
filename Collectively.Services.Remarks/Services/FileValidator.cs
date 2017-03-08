using System;
using System.IO;
using NLog;
using Structure.Sketching;
using File = Collectively.Services.Remarks.Domain.File;

namespace Collectively.Services.Remarks.Services
{
    public class FileValidator : IFileValidator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool IsImage(File file)
        {
            try
            {
                using (var stream = new MemoryStream(file.Bytes))
                {
                    var image = new Image(stream);

                    return image.Width > 0 && image.Height > 0;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error while reading image from stream");
                
                return false;
            }
        }
    }
}