using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IFileValidator
    {
        bool IsImage(File file);
    }
}