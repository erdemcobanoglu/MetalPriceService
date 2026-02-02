using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Entities
{
    public sealed class ServiceSchedule
    {
        public int Id { get; set; } = 1; // tek kayıt
        public string MorningTime { get; set; } = "09:00";
        public string EveningTime { get; set; } = "21:00";
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
