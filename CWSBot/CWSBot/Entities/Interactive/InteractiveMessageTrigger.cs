using System.Collections.Generic;

namespace CWSBot.Entities.Interactive
{
    /// <summary>
    ///     The trigger for a message
    /// </summary>
    public class InteractiveMessageTrigger : InteractiveMessageTrigger<string>
    {
        /// <summary>
        ///     Creates a new instance of <see cref="T:InteractiveMessageTrigger"/> whose generic argument is <see cref="T:System.String"/>.
        /// <param name="trigger">
        ///     The trigger of the message
        /// </param>
        /// <param name="conditions">
        ///     A collection of <see cref="T:ITriggerCondition"/>.
        /// </param>
        /// </summary>
        public InteractiveMessageTrigger(string trigger, params ITriggerCondition[] conditions)
            : base(trigger, conditions) {}
    }

    /// <summary>
    ///     The trigger for a message
    /// </summary>
    public class InteractiveMessageTrigger<T>
    {
        /// <value>
        /// A collection of <see cref="T:ITriggerCondtion"/>.
        /// </value>
        public IReadOnlyCollection<ITriggerCondition> Conditions { get; }

        /// <value>
        ///     The Trigger
        /// </value>
        public T Trigger { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="T:InteractiveMessageTrigger"/>.
        /// <param name="trigger">
        ///     The trigger of the message
        /// </param>
        /// <param name="conditions">
        ///     A collection of <see cref="T:ITriggerCondition"/>.
        /// </param>
        /// </summary>
        public InteractiveMessageTrigger(T trigger, params ITriggerCondition[] conditions)
        {
            this.Trigger = trigger;
            this.Conditions = conditions;
        }
    }
}