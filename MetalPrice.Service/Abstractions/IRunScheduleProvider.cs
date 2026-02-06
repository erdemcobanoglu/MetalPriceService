using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Abstractions
{
    public interface IRunScheduleProvider
    {
        Task<IReadOnlyList<TimeOnly>> GetTimesAsync(CancellationToken ct);
    }
}
