using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetalPrice.Service.Abstractions
{
    public interface IPriceSnapshotJob
    { 
        Task RunOnceAsync(string slot, IReadOnlyList<TimeOnly> times, CancellationToken ct);
    }
}
