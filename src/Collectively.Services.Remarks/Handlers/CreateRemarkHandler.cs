using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Files;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Lockbox.Client.Extensions;
using Serilog;
using RawRabbit;
using Collectively.Messages.Commands.Models;
using Collectively.Common.Locations;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Handlers
{
    public class CreateRemarkHandler : ICommandHandler<CreateRemark>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly IRemarkService _remarkService;
        private readonly IGroupService _groupService;
        private readonly ISocialMediaService _socialMediaService;
        private readonly ILocationService _locationService;
        private readonly IResourceFactory _resourceFactory;
        private readonly ICreateRemarkPolicy _policy;

        public CreateRemarkHandler(IHandler handler,
            IBusClient bus, 
            IFileResolver fileResolver, 
            IFileValidator fileValidator,
            IRemarkService remarkService,
            IGroupService groupService,
            ISocialMediaService socialMediaService,
            ILocationService locationService,
            IResourceFactory resourceFactory,
            ICreateRemarkPolicy policy)
        {
            _handler = handler;
            _bus = bus;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _remarkService = remarkService;
            _groupService = groupService;
            _socialMediaService = socialMediaService;
            _locationService = locationService;
            _resourceFactory = resourceFactory;
            _policy = policy;
        }

        public async Task HandleAsync(CreateRemark command)
        {
            var address = "";
            await _handler
                .Validate(async () =>  
                {
                    await _policy.ValidateAsync(command.UserId);
                    if(command.GroupId.HasValue)
                    {
                        await _groupService.ValidateIfRemarkCanBeCreatedOrFailAsync(command.GroupId.Value,
                            command.UserId, command.Latitude, command.Longitude);
                    }
                    var locations = await _locationService.GetAsync(command.Latitude, command.Longitude);
                    if (locations.HasNoValue || locations.Value.Results == null || !locations.Value.Results.Any())
                    {
                        throw new ServiceException($"Address was not found for remark with id: '{command.RemarkId}' " +
                            $"latitude: {command.Latitude}, longitude:  {command.Longitude}.");
                    }
                    address = locations.Value.Results.First().FormattedAddress;
                })
                .Run(async () =>
                {
                    Logger.Debug($"Handle {nameof(CreateRemark)} command, userId: {command.UserId}, " +
                                 $"category: {command.Category}, latitude: {command.Latitude}, " +
                                 $"longitude:  {command.Longitude}.");

                    var location = Domain.Location.Create(command.Latitude, command.Longitude, address);
                    var offering = command.Offering;
                    if (offering != null)
                    {
                        Logger.Information($"Offering for remark: '{command.RemarkId}' " + 
                            $"with price: '{offering.Price} {offering.Currency}'.");
                    }

                    await _remarkService.CreateAsync(command.RemarkId, command.UserId, command.Category,
                            location, command.Description, command.Tags, command.GroupId,
                            offering?.Price, offering?.Currency, offering?.StartDate, offering?.EndDate);
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
                        Photos = new List<Collectively.Messages.Commands.Models.File>
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