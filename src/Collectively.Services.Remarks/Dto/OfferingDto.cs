using System;

namespace Collectively.Services.Remarks.Dto
{
    public class OfferingDto
    {
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }        
    }
}