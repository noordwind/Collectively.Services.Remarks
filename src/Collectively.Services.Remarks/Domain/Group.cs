using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class Group : IdentifiableEntity
    {
        private IDictionary<string,ISet<string>> _criteria = new Dictionary<string,ISet<string>>();
        private ISet<GroupMember> _members = new HashSet<GroupMember>();
        private ISet<string> _tags = new HashSet<string>();
        public string Name { get; protected set; } 
        public Guid? OrganizationId { get; protected set; } 
        public bool IsPublic { get; protected set; }
        public string State { get; protected set; }
        public IEnumerable<GroupMember> Members
        {
            get { return _members; }
            protected set { _members =  new HashSet<GroupMember>(value); }
        }
        public IDictionary<string,ISet<string>> Criteria
        {
            get { return _criteria; }
            protected set { _criteria = new Dictionary<string,ISet<string>>(value); }
        }
        public IEnumerable<string> Tags
        {
            get { return _tags; }
            protected set { _tags =  new HashSet<string>(value); }
        }

        protected Group()
        {
        }

        public Group(Guid id, string name, bool isPublic, string state, 
            string userId, IDictionary<string, ISet<string>> criteria, 
            IEnumerable<string> tags, Guid? organizationId = null)
        {
            Id = id;
            Name = name;
            IsPublic = isPublic;
            State = state;
            OrganizationId = organizationId;
            _members.Add(new GroupMember(userId, "owner", true));
            Criteria = criteria ?? new Dictionary<string,ISet<string>>();
            Tags = tags ?? Enumerable.Empty<string>();
        }

        public void AddMember(string userId, string role, bool isActive)
        {
            if (_members.Any(x => x.UserId == userId))
            {
                throw new DomainException(OperationCodes.GroupMemberAlreadyExists, 
                    $"User: '{userId}' is already a group: '{Id}' [{Name}] member.");
            }
            _members.Add(new GroupMember(userId, role, isActive));
        }
    }
}