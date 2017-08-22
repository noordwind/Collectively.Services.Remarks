using System;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class Report : IdentifiableEntity, ITimestampable
    {
        public Guid RemarkId { get; protected set; }
        public Guid? ResourceId { get; protected set; }
        public string Type { get; protected set; }
        public string UserId { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        public Report()
        {
        }
    
        public Report(Guid remarkId, Guid? resourceId, 
            string type, string userId)
        {
            RemarkId = remarkId;
            ResourceId = resourceId;
            Type = type;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}