using System;
using  Collectively.Common.Queries;

namespace Collectively.Services.Remarks.Queries
{
    public class GetRemarkPhoto : IQuery
    {
        public Guid Id { get; set; }
        public string Size { get; set; }
    }
}