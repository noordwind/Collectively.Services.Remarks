using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class GroupRemark : IdentifiableEntity
    {
        private ISet<GroupRemarkState> _remarks = new HashSet<GroupRemarkState>();
        public Guid GroupId { get; protected set; }
        public IEnumerable<GroupRemarkState> Remarks
        {
            get { return _remarks; }
            protected set { _remarks =  new HashSet<GroupRemarkState>(value); }
        }

        protected GroupRemark()
        {
        }

        public GroupRemark(Guid groupId)
        {
            Id = Guid.NewGuid();
            GroupId = groupId;
        }

        public void AddRemark(Guid id)
            => _remarks.Add(GroupRemarkState.Create(id));

        public void DeleteRemark(Guid id)
            => _remarks.Remove(_remarks.SingleOrDefault(x => x.Id == id));

        public void Assign(Guid id)
            => _remarks.SingleOrDefault(x => x.Id == id)?.Assign();

        public void Deny(Guid id)
            => _remarks.SingleOrDefault(x => x.Id == id)?.Deny();

        public void Take(Guid id)
            => _remarks.SingleOrDefault(x => x.Id == id)?.Take();

        public void Clear(Guid id)
            => _remarks.SingleOrDefault(x => x.Id == id)?.Clear();
    }
}