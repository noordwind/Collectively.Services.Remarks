using AutoMapper;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Queries;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Shared.Dto;

namespace Coolector.Services.Remarks.Modules
{
    public class RemarkModule : ModuleBase
    {
        public RemarkModule(IRemarkService remarkService, IMapper mapper) : base(mapper, "remarks")
        {
            Get("", async args => await FetchCollection<BrowseRemarks, Remark>
                (async x => await remarkService.BrowseAsync(x))
                .MapTo<RemarkDto>()
                .HandleAsync());

            Get("categories", async args => await FetchCollection<BrowseCategories, Category>
                (async x => await remarkService.BrowseCategoriesAsync(x))
                .MapTo<RemarkCategoryDto>()
                .HandleAsync());

            Get("tags", async args => await FetchCollection<BrowseTags, Tag>
                (async x => await remarkService.BrowseTagsAsync(x))
                .MapTo<TagDto>()
                .HandleAsync());

            Get("{id}", async args => await Fetch<GetRemark, Remark>
                (async x => await remarkService.GetAsync(x.Id))
                .MapTo<RemarkDto>()
                .HandleAsync());
        }
    }
}