using System;
using  Collectively.Common.Queries;

namespace Collectively.Services.Remarks.Queries
{
    public class GetRemark : IQuery
    {
        public Guid Id { get; set; }
    }
}