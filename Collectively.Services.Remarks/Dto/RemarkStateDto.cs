using System;

namespace Collectively.Services.Remarks.Dto
{
    public class RemarkStateDto
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public RemarkUserDto User { get; set; }
        public string Description { get; set; }
        public LocationDto Location { get; set; }
        public FileDto Photo { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Removed { get; set; }
    }
}