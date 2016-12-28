using System.Threading.Tasks;
using Coolector.Common.Types;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Queries;

namespace Coolector.Services.Remarks.Repositories
{
    public interface ITagRepository
    {
         Task<Maybe<Tag>> GetAsync(string name);
         Task<Maybe<PagedResult<Tag>>> BrowseAsync(BrowseTags query);
         Task AddAsync(Tag tag);
    }
}