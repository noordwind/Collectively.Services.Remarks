using System.Threading.Tasks;

namespace Collectively.Services.Remarks.Policies
{
    public interface ICreateRemarkPolicy : IPolicy
    {
         Task ValidateAsync(string userId);
    }
}