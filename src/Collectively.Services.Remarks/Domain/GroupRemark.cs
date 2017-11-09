using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class GroupRemark : IdentifiableEntity
    {
        public Guid GroupId { get; protected set; }
        public Guid RemarkId { get; protected set; }

        protected GroupRemark()
        {
        }

        public GroupRemark(Guid groupId, Guid remarkId)
        {
            Id = Guid.NewGuid();
            GroupId = groupId;
            RemarkId = remarkId;
        }
    }
}