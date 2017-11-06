using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Dto;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories;

namespace Collectively.Services.Remarks.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task AddOrUpdateAsync(IEnumerable<TagDto> tags)
        {
            var allTags = await _tagRepository.BrowseAsync(new BrowseTags
            {
                Results = int.MaxValue
            });
            var newTags = new List<Tag>();
            foreach (var tag in tags)
            {
                var existingTag = allTags.Value.Items.SingleOrDefault(x => x.Name == tag.Name);
                if (existingTag != null)
                {
                    continue;
                }
                if (newTags.Any(x => x.Name == tag.Name))
                {
                    continue;
                }
                var newTag = new Tag(tag.Id, tag.Name, tag.Translations.Select(x => new TranslatedTag(x.Id, x.Name, x.Culture)));
                newTags.Add(newTag);
            }
            await _tagRepository.AddAsync(newTags);
        }
    }
}