using System;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkStateService
    {
        Task ResolveAsync(Guid id, string userId, string description = null, Location location = null, 
            File photo = null, bool validateLocation = false);
        Task ProcessAsync(Guid id, string userId, string description = null, Location location = null);
        Task RenewAsync(Guid id, string userId, string description = null, Location location = null);
        Task CancelAsync(Guid id, string userId, string description = null, Location location = null);
    }
}