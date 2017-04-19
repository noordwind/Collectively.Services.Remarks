using System;
using System.Threading.Tasks;

namespace Collectively.Services.Remarks.Policies
{
    public interface IProcessRemarkPolicy : IPolicy
    {
        Task ValidateAsync(Guid remarkId, string userId);
    }
}