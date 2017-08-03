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
        public string Name { get; protected set; } 
        public Guid? OrganizationId { get; protected set; } 
        public bool IsPublic { get; protected set; }
        public string State { get; protected set; }
        public ISet<GroupMember> Members
        {
            get { return _members; }
            protected set { _members =  new HashSet<GroupMember>(value); }
        }
        public IDictionary<string,ISet<string>> Criteria
        {
            get { return _criteria; }
            protected set { _criteria = new Dictionary<string,ISet<string>>(value); }
        }

        protected Group()
        {
        }

        public Group(Guid id, string name, bool isPublic, string state, 
            string userId, IDictionary<string, ISet<string>> criteria, Guid? organizationId = null)
        {
            Id = id;
            Name = name;
            IsPublic = isPublic;
            State = state;
            OrganizationId = organizationId;
            _members.Add(new GroupMember(userId, "owner", true));
            _criteria = criteria ?? new Dictionary<string,ISet<string>>();
        }
    }
}