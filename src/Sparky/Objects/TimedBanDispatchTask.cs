using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sparky.Objects
{
    public class TimedBanDispatchTask : TimedDispatchTask
    {
        public TimedBanDispatchTask(Func<CancellationToken, Task<DispatchResult>> action, DateTimeOffset dueTo) : base(action, dueTo)
        {
        }
    }
}