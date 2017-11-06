using System.Threading.Tasks;
using Collectively.Common.Files;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Serilog;
using RawRabbit;
using RemarkState = Collectively.Services.Remarks.Domain.RemarkState;
using System.Linq;
using Collectively.Services.Remarks.Dto;
using System;
using Collectively.Messages.Events.Models;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Handlers
{
    public class CreateTagsHandler : ICommandHandler<CreateTags>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly ITagService _tagService;

        public CreateTagsHandler(IHandler handler,
            IBusClient bus,
            ITagService tagService)
        {
            _handler = handler;
            _bus = bus;
            _tagService = tagService;
        }

        public async Task HandleAsync(CreateTags command)
        {
            var id = Guid.NewGuid();
            var tags = command.Tags.Select(x => new TagDto
            {
                Id = Guid.NewGuid(),
                Name = x.Name.TrimToLower().Replace(" ", string.Empty),
                Translations = x.Translations.Select(t => new TranslatedTagDto
                {
                    Id = Guid.NewGuid(),
                    Name = t.Name.TrimToLower().Replace(" ", string.Empty),
                    Culture = t.Culture.ToLowerInvariant()
                }).ToList()
            }).ToList();
            await _handler
                .Run(async () => await _tagService.AddOrUpdateAsync(tags))
                .OnSuccess(async () => 
                {
                    var createdTags = tags.Select(x => new Tag
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Translations = x.Translations.Select(t => new TranslatedTag
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Culture = t.Culture
                        }).ToList()
                    }).ToList();
                    await _bus.PublishAsync(new TagsCreated(command.Request.Id, command.UserId, createdTags));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new CreateTagsRejected(command.Request.Id,
                    command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while creating the tags.");
                    await _bus.PublishAsync(new CreateTagsRejected(command.Request.Id,
                    command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}