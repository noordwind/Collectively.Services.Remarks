using System;
using System.Collections.Generic;

namespace Collectively.Services.Remarks.Dto
{
    public class UserDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<Guid> FavoriteRemarks { get; set; }
    }
}