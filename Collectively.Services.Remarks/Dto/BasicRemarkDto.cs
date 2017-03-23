using System;

namespace Collectively.Services.Remarks.Dto
{
    public class BasicRemarkDto
    {
        public Guid Id { get; set; }
        public RemarkUserDto Author { get; set; }
        public RemarkCategoryDto Category { get; set; }
        public LocationDto Location { get; set; }
        public RemarkStateDto State { get; set; }
        public string SmallPhotoUrl { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Rating { get; set; }
        public bool Resolved { get; set; }
        public int CommentsCount { get; set; }
    }
}