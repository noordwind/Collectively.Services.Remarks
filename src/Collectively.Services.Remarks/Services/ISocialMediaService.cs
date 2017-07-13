using System;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface ISocialMediaService
    {
        Task PublishRemarkCreatedAsync(Guid remarkId, string culture, params UserSocialMedia[] socialMedia);
    }
}