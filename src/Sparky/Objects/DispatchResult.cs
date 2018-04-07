using System;

namespace Sparky.Objects
{
    public abstract class DispatchResult
    {
        public DateTimeOffset? DispatchedAt { get; }

        public DispatchType DispatchType { get; }

        public DispatchResult(DispatchType type, DateTimeOffset? dispatchedAt = null)
        {
            this.DispatchType = type;
            this.DispatchedAt = dispatchedAt;
        }
    }
}