using System;
using System.Threading.Tasks;
using Coolector.Services.Remarks.Domain;

namespace Coolector.Services.Remarks.Services
{
    public interface ISocialMediaService
    {
        Task PublishRemarkCreatedAsync(Guid remarkId, string culture, params UserSocialMedia[] socialMedia);
    }
}