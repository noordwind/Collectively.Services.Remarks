using System;

namespace Collectively.Services.Remarks.Dto
{
    public class OfferingProposalDto
    {
        public Guid Id { get; set; }
        public Guid RemarkId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }        
    }
}