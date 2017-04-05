using System;

namespace Collectively.Services.Remarks.Dto
{
    public class ParticipantDto
    {
        public RemarkUserDto User { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }        
    }
}