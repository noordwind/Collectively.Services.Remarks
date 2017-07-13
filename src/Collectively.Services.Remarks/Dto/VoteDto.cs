using System;

namespace Collectively.Services.Remarks.Dto
{
    public class VoteDto
    {
        public string UserId { get; set; }
        public bool Positive { get; set; }
        public DateTime CreatedAt { get; set; }        
    }
}