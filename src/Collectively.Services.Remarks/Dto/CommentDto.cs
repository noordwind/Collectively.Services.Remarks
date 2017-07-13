using System;
using System.Collections.Generic;

namespace Collectively.Services.Remarks.Dto
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public RemarkUserDto User { get; set; }
        public string Text { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Removed { get; set; }
        public IList<CommentHistoryDto> History { get; set; }
        public IList<VoteDto> Votes { get; set; }        
    }
}