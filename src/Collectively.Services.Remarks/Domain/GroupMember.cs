namespace Collectively.Services.Remarks.Domain
{
    public class GroupMember
    {
        public string UserId { get; protected set; }
        public string Role { get; protected set; }
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