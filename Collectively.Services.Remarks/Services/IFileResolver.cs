using  Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IFileResolver
    {
        Maybe<File> FromBase64(string base64, string name, string contentType);
    }
}