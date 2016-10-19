using System;
using System.Drawing;
using System.IO;
using File = Coolector.Services.Remarks.Domain.File;

namespace Coolector.Services.Remarks.Services
{
    public class FileValidator : IFileValidator
    {
        public bool IsImage(File file)
        {
            try
            {
                using (var stream = new MemoryStream(file.Bytes))
                {
                    var image = Image.FromStream(stream);

                    return image.Width > 0 && image.Height > 0;
                }
            }
            catch (Exception exception)
            {
                return false;
            }
        }
    }
}