using System;

namespace Coolector.Services.Remarks.Shared.Dto
{
    public class BasicRemarkDto
    {
        public Guid Id { get; set; }
        public RemarkAuthorDto Author { get; set; }
        public RemarkCategoryDto Category { get; set; }
        public LocationDto Location { get; set; }
        public string SmallPhotoUrl { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Resolved { get; set; }
        public int Rating { get; set; }
    }
}