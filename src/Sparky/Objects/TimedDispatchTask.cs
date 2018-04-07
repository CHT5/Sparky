using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sparky.Objects
{
    public class TimedDispatchTask
    {
        private readonly CancellationTokenSource _cancelTokenSource;

        private readonly Task _dispatchTask;

        public Task Task { get; }

        public DateTimeOffset DueTo { get; }

        private Func<DispatchResult, Task> _dispatched;

        public event Func<DispatchResult, Task> Dispatched 
        { 
            add
            {
                this._dispatched += value;
            }
            remove
            {
                this._dispatched -= value;
            }
        }

        public TimedDispatchTask(Func<CancellationToken, Task<DispatchResult>> action, DateTimeOffset dueTo)
        {
            this._cancelTokenSource = new CancellationTokenSource();
            var task = Task.Run(async () => await action(this._cancelTokenSource.Token));
            this._dispatchTask = task.ContinueWith(t => this._dispatched?.Invoke(t.Result)); // Invoke the dispatched event after it's done
            this.Task = task;
            this.DueTo = dueTo;
        }

        public void Cancel()
            => this._cancelTokenSource.Cancel();

        public void Start()
            => this.Task.Start();
    }
}