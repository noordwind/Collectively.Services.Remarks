using System;

namespace Coolector.Services.Remarks.Services
{
    public class UniqueNumberGenerator : IUniqueNumberGenerator
    {
        public long Generate() => DateTime.UtcNow.Ticks;
    }
}