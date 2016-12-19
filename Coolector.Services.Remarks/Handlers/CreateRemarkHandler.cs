using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Commands;
using Coolector.Common.Domain;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Shared.Commands;
using Coolector.Services.Remarks.Shared.Commands.Models;
using Coolector.Services.Remarks.Shared.Events;
using Coolector.Services.Users.Shared.Commands;
using Lockbox.Client.Extensions;
using NLog;
using RawRabbit;
using RemarkFile = Coolector.Services.Remarks.Shared.Events.Models.RemarkFile;
using Coolector.Services.Remarks.Settings;

namespace Coolector.Services.Remarks.Handlers
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

        public CreateRemarkHandler(IHandler handler,
            IBusClient bus, 
            IFileResolver fileResolver, 
            IFileValidator fileValidator,
            IRemarkService remarkService,
            ISocialMediaService socialMediaService)
        {
            _handler = handler;
            _bus = bus;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _remarkService = remarkService;
            _socialMediaService = socialMediaService;
        }

        public async Task HandleAsync(CreateRemark command)
        {
            await _handler
                .Run(async () =>
                {
                    Logger.Debug($"Handle {nameof(CreateRemark)} command, userId: {command.UserId}, " +
                                 $"category: {command.Category}, lat/lng: {command.Latitude} {command.Longitude}");
                    var file = _fileResolver.FromBase64(command.Photo.Base64, command.Photo.Name, command.Photo.ContentType);
                    if (file.HasNoValue)
                    {
                        Logger.Error($"File cannot be resolved from base64, photoName:{command.Photo.Name}, " +
                                    $"contentType:{command.Photo.ContentType}, userId:{command.UserId}");
                        throw new ServiceException(OperationCodes.CannotConvertFile);
                    }

                    var isImage = _fileValidator.IsImage(file.Value);
                    if (!isImage)
                    {
                        Logger.Warn($"File is not an image! name:{file.Value.Name}, " +
                                    $"contentType:{file.Value.ContentType}, userId:{command.UserId}");
                        throw new ServiceException(OperationCodes.InvalidFile);
                    }

                    var location = Location.Create(command.Latitude, command.Longitude, command.Address);
                    await _remarkService.CreateAsync(command.RemarkId, command.UserId, command.Category,
                        file.Value, location, command.Description);
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    await PublishOnSocialMediaAsync(command.RemarkId, command.Request.Culture, command.SocialMedia);
                    await _bus.PublishAsync(new RemarkCreated(command.Request.Id, command.RemarkId,
                        command.UserId, remark.Value.Author.Name,
                        new RemarkCreated.RemarkCategory(remark.Value.Category.Id, remark.Value.Category.Name),
                        new RemarkCreated.RemarkLocation(remark.Value.Location.Address, command.Latitude, command.Longitude),
                        remark.Value.Photos.Select(x => new RemarkFile(x.Name, x.Size, x.Url, x.Metadata)).ToArray(),
                        command.Description, remark.Value.CreatedAt));
                })
                .OnCustomError(ex => _bus.PublishAsync(new CreateRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while creating remark.");
                    await _bus.PublishAsync(new CreateRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }

        private async Task PublishOnSocialMediaAsync(Guid remarkId, string culture, List<SocialMedia> socialMedia)
        {
            if (socialMedia == null || !socialMedia.Any())
                return;

            var userSocialMedia = socialMedia
                .Where(x => x.Name.NotEmpty() && x.Publish)
                .Select(x => UserSocialMedia.Create(x.Name, x.AccessToken))
                .ToArray();

            await _socialMediaService.PublishRemarkCreatedAsync(remarkId, culture, userSocialMedia);
        }
    }
}