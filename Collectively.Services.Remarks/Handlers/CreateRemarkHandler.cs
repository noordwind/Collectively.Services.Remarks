using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Commands.Remarks.Models;
using Collectively.Messages.Events.Remarks;
using Lockbox.Client.Extensions;
using NLog;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class CreateRemarkHandler : ICommandHandler<CreateRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly IRemarkService _remarkService;
        private readonly ISocialMediaService _socialMediaService;
        private readonly IResourceFactory _resourceFactory;

        public CreateRemarkHandler(IHandler handler,
            IBusClient bus, 
            IFileResolver fileResolver, 
            IFileValidator fileValidator,
            IRemarkService remarkService,
            ISocialMediaService socialMediaService,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _remarkService = remarkService;
            _socialMediaService = socialMediaService;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(CreateRemark command)
        {
            await _handler
                .Run(async () =>
                {
                    Logger.Debug($"Handle {nameof(CreateRemark)} command, userId: {command.UserId}, " +
                                 $"category: {command.Category}, latitude: {command.Latitude}, " +
                                 $"longitude:  {command.Longitude}.");

                    var location = Domain.Location.Create(command.Latitude, command.Longitude, command.Address);
                    await _remarkService.CreateAsync(command.RemarkId, command.UserId, command.Category,
                            location, command.Description, command.Tags);
                })
                .OnSuccess(async () =>
                {
                    await PublishOnSocialMediaAsync(command.RemarkId, command.Request.Culture, command.SocialMedia);
                    var resource = _resourceFactory.Resolve<RemarkCreated>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkCreated(command.Request.Id, resource, 
                        command.UserId, command.RemarkId));
                })
                .OnCustomError(ex => _bus.PublishAsync(new CreateRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while creating remark.");
                    await _bus.PublishAsync(new CreateRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .Next()
                .Run(async () =>
                {
                    if(command.Photo == null)
                    {
                        return;
                    }

                    await _bus.PublishAsync(new AddPhotosToRemark
                    {
                        RemarkId = command.RemarkId,
                        Request = Request.New<AddPhotosToRemark>(),
                        UserId = command.UserId,
                        Photos = new List<Collectively.Messages.Commands.Remarks.Models.RemarkFile>
                        {
                            command.Photo
                        }
                    });
                })
                .Next()
                .ExecuteAllAsync();
        }

        private async Task PublishOnSocialMediaAsync(Guid remarkId, string culture, IList<SocialMedia> socialMedia)
        {
            if (socialMedia == null || !socialMedia.Any())
            {
                Logger.Debug($"Remark with id: '{remarkId}' will not be published on social media.");

                return;
            }

            Logger.Debug($"Remark with id: '{remarkId}' will be published on social media.");
            var userSocialMedia = socialMedia
                .Where(x => x.Name.NotEmpty() && x.Publish)
                .Select(x => Domain.UserSocialMedia.Create(x.Name, x.AccessToken))
                .ToArray();

            await _socialMediaService.PublishRemarkCreatedAsync(remarkId, culture, userSocialMedia);
        }
    }
}