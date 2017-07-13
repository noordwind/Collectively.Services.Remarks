using System;
using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using Collectively.Messages.Commands.Users;
using NLog;
using RawRabbit;

namespace Collectively.Services.Remarks.Services
{
    public class SocialMediaService : ISocialMediaService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IBusClient _bus;
        private readonly ILocalizedResourceService _localizedResourceService;
        private readonly IRemarkRepository _remarkRepository;
        private readonly GeneralSettings _generalSettings;

        public SocialMediaService(IBusClient bus,
            ILocalizedResourceService localizedResourceService,
            IRemarkRepository remarkRepository,
            GeneralSettings generalSettings)
        {
            _bus = bus;
            _localizedResourceService = localizedResourceService;
            _remarkRepository = remarkRepository;
            _generalSettings = generalSettings;
        }

        public async Task PublishRemarkCreatedAsync(Guid remarkId, string culture, params UserSocialMedia[] socialMedia)
        {
            var remark = await _remarkRepository.GetByIdAsync(remarkId);
            if (remark.HasNoValue)
            {
                return;
            }

            foreach (var service in socialMedia)
            {
                Logger.Debug($"Remark with id: '{remarkId}' will be published on: '{service.Name}'.");
                switch (service.Name)
                {
                    case "facebook":
                        var message = await _localizedResourceService.TranslateAsync("facebook:new_remark",
                            culture, $"{_generalSettings.RemarkDetailsUrl}{remarkId}");
                        if (message.HasNoValue)
                        {
                            Logger.Debug($"Remark with id: '{remarkId}' will not be published " +
                                         $"on: '{service.Name}' as the translated message was not found.");
                            break;
                        }
                        await _bus.PublishAsync(new PostOnFacebookWall
                        {
                            Request = Request.New<PostOnFacebookWall>(),
                            UserId = remark.Value.Author.UserId,
                            AccessToken = service.AccessToken,
                            Message = message.Value
                        });
                        Logger.Debug($"Remark with id: '{remarkId}' was published on: '{service.Name}'.");
                        break;
                }
            }
        }
    }
}