using System;
using System.Collections.Generic;

namespace Coolector.Services.Remarks.Shared.Dto
{
    public class RemarkDto
    {
        public Guid Id { get; set; }
        public RemarkAuthorDto Author { get; set; }
        public RemarkCategoryDto Category { get; set; }
        public LocationDto Location { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public RemarkAuthorDto Resolver { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool Resolved { get; set; }
        public int Rating { get; set; }
        public IList<FileDto> Photos { get; set; }
        public IList<string> Tags { get; set; }
        public IList<VoteDto> Votes { get; set; }
    }
}