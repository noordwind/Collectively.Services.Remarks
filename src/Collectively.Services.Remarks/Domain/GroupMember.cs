using System.Linq;

namespace Collectively.Services.Remarks.Domain
{
    public class GroupMember
    {
        private static readonly string[] _administrativeRoles = new [] {"moderator", "administrator", "owner"};
        public string UserId { get; protected set; }
        public string Role { get; protected set; }
        public bool HasAdministrativeRole => _administrativeRoles.Contains(Role);
        public bool IsActive { get; protected set; }    

        protected GroupMember()
        {
        }

        public GroupMember(string userId, string role, bool isActive)
        {
            UserId = userId;
            Role = role;
            IsActive = isActive;
        }
    }
}