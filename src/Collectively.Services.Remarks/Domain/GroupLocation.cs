using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class GroupLocation : IdentifiableEntity
    {
        private ISet<string> _locations = new HashSet<string>();
        private ISet<string> _tags = new HashSet<string>();
        public Guid GroupId { get; protected set; }
        public IEnumerable<string> Locations
        {
            get { return _locations; }
            protected set { _locations =  new HashSet<string>(value); }
        }
        public IEnumerable<string> Tags
        {
            get { return _tags; }
            protected set { _tags = new HashSet<string>(value); }
        }

        protected GroupLocation()
        {
        }

        public GroupLocation(Guid groupId, IEnumerable<string> locations,
            IEnumerable<string> tags)
        {
            Id = Guid.NewGuid();
            GroupId = groupId;
            Locations = locations.Select(x => x.ToLowerInvariant());
            Tags = tags;
        } 
    }
}