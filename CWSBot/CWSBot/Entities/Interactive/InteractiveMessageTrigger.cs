using System.Collections.Generic;

namespace CWSBot.Entities.Interactive
{
    public class InteractiveMessageTrigger : InteractiveMessageTrigger<string>
    {
        public InteractiveMessageTrigger(string trigger, params ITriggerCondition[] conditions)
            : base(trigger, conditions) {}
    }

    public class InteractiveMessageTrigger<T>
    {
        public IEnumerable<ITriggerCondition> Conditions { get; }

        public T Trigger { get; }

        public InteractiveMessageTrigger(T trigger, params ITriggerCondition[] conditions)
        {
            this.Trigger = trigger;
            this.Conditions = conditions;
        }
    }
}