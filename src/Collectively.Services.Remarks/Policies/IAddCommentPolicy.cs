using System;
using System.Threading.Tasks;

namespace Collectively.Services.Remarks.Policies
{
    public interface IAddCommentPolicy : IPolicy
    {
        Task ValidateAsync(Guid remarkId, string userId);
    }
}