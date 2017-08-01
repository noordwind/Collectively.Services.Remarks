using System;
using System.Collections.Generic;

namespace Collectively.Services.Remarks.Dto
{
    public class GroupDto
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Name { get; set; }  
        public bool IsPublic { get; set; }
        public string State { get; set; }  
        public IList<GroupMemberDto> Members { get; set; }
        public IList<string> Locations { get; set; }
        public IDictionary<string,string> Criteria { get; set; }
    }
}