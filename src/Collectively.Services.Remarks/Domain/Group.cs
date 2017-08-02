using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class Group : IdentifiableEntity
    {
        private IDictionary<string,string> _criteria = new Dictionary<string,string>();
        private ISet<GroupMember> _members = new HashSet<GroupMember>();
        private ISet<string> _locations = new HashSet<string>();
        public string Name { get; protected set; } 
        public Guid? OrganizationId { get; protected set; } 
        public bool IsPublic { get; protected set; }
        public string State { get; protected set; }
        public ISet<GroupMember> Members
        {
            get { return _members; }
            protected set { _members =  new HashSet<GroupMember>(value); }
        }
        public IEnumerable<string> Locations
        {
            get { return _locations; }
            protected set { _locations = new HashSet<string>(value); }
        }
        public IDictionary<string,string> Criteria
        {
            get { return _criteria; }
            protected set { _criteria = new Dictionary<string,string>(value); }
        }

        protected Group()
        {
        }

        public Group(Guid id, string name, bool isPublic, string state, 
            string userId, IDictionary<string, string> criteria, IEnumerable<string> locations,
            Guid? organizationId = null)
        {
            Id = id;
            Name = name;
            IsPublic = isPublic;
            State = state;
            OrganizationId = organizationId;
            _members.Add(new GroupMember(userId, "owner", true));
            _criteria = criteria ?? new Dictionary<string,string>();
            _locations = locations == null ? new HashSet<string>() : new HashSet<string>(locations);
        }
    }
}