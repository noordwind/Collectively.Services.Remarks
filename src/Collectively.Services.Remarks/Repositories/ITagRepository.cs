using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;

namespace Collectively.Services.Remarks.Repositories
{
    public interface ITagRepository
    {
         Task<Maybe<Tag>> GetAsync(string name);
         Task<Maybe<PagedResult<Tag>>> BrowseAsync(BrowseTags query);
         Task AddAsync(Tag tag);
    }
}