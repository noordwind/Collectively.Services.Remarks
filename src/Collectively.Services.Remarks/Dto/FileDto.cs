using System;

namespace Collectively.Services.Remarks.Dto
{
    public class FileDto
    {
        public Guid GroupId { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Url { get; set; }
        public string Metadata { get; set; }
        public RemarkUserDto User { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}