using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sparky.Objects
{
    public class TimedSpecialRoleDispatchTask : TimedDispatchTask
    {
        public TimedSpecialRoleDispatchTask(Func<CancellationToken, Task<DispatchResult>> action, DateTimeOffset dueTo) : base(action, dueTo)
        {
        }
    }
}