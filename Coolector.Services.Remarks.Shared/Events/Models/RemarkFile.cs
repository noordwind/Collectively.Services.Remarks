using System;

namespace Coolector.Services.Remarks.Shared.Events.Models
{
    public class RemarkFile
    {
        public Guid GroupId { get; }
        public string Name { get; }
        public string Size { get; }
        public string Url { get; }
        public string Metadata { get; }
        
        protected RemarkFile() {}

        public RemarkFile(Guid groupId, string name, string size, string url, string metadata)
        {
            GroupId = groupId;
            Name = name;
            Size = size;
            Url = url;
            Metadata = metadata;
        }
    }
}