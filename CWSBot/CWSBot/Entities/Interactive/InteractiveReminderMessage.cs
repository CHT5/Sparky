using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CWSBot.Interaction;
using CWSBot.Misc;
using CWSBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CWSBot.Entities.Interactive
{
    public sealed class InteractiveReminderMessage : IInteractiveMessage
    {
        // Emoji
        private const string One = "1⃣";
        private const string Two = "2⃣";
        private const string Three = "3⃣";
        private const string Four = "4⃣";
        private const string Five = "5⃣";
        private const string Delete = "❌";
        private const string Stop = "⏹";
        private const string Reload = "🔄";

        // Variables passed in by the user
        private readonly IUser _requestUser;
        private readonly IUser _queryUser;
        private readonly IGuild _guild;
        private readonly IMessageChannel _channel;

        // IInteractiveMessage implementations
        IEnumerable<InteractiveMessageTrigger> IInteractiveMessage.Triggers => this._triggers;
        event Func<IInteractiveMessage, Task> IInteractiveMessage.Exited
        {
            add => this._exited += value;
            remove => this._exited += value;
        }

        // Other vars
        private List<InteractiveMessageTrigger> _triggers;
        private Embed _embed;
        private IUserMessage _message;
        private ConcurrentDictionary<int, (Reminder Reminder, bool State)> _selectionStates;
        private RemindService _remindService;
        private bool _modRequested => this._requestUser.Id != this._queryUser.Id;
        private Func<IInteractiveMessage, Task> _exited;

        /// <summary>
        /// <param name="channel">
        ///     The channel the message should be sent to.
        /// </param>
        /// <param name="requestUser">
        ///     The user who requested the message.
        ///     Only this user will be able to use the buttons.
        /// </param>
        /// <param name="queryUser">
        ///     The user whose reminders to show.
        ///     By default the requestUser's reminders will be queried.
        /// </param>
        /// </summary>
        public InteractiveReminderMessage(ITextChannel channel, IUser requestUser, IUser queryUser = null)
        {
            this._triggers = new List<InteractiveMessageTrigger>();
            this._selectionStates = new ConcurrentDictionary<int, (Reminder Reminder, bool State)>();
            this._requestUser = requestUser;
            this._queryUser = queryUser ?? requestUser;
            this._guild = channel.Guild;
            this._channel = channel;
        }

        // Implementation Methods
        async Task IInteractiveMessage.SetUpAsync()
        {
            this.UpdateCache(this._queryUser, this._guild);
            this._embed = this.GenerateEmbed();
            this._message = await this._channel.SendMessageAsync("", embed: this._embed);
            this._triggers = ConfigureTriggers().ToList();
            await AddReactionsAsync(this._message);
        }

        void IInteractiveMessage.AddServiceProvider(IServiceProvider provider)
            => this._remindService = provider.GetService<RemindService>();

        Task IInteractiveMessage.OnTriggerReceived(SocketReaction reaction)
        {
            switch (reaction.Emote.Name)
            {
                case One:
                    if (this._selectionStates.Count() < 1) return Task.CompletedTask;
                    SwitchPosition(0);
                    return UpdateEmbedAsync();

                case Two:
                    if (this._selectionStates.Count() < 2) return Task.CompletedTask;
                    SwitchPosition(1);
                    return UpdateEmbedAsync();

                case Three:
                    if (this._selectionStates.Count() < 3) return Task.CompletedTask;
                    SwitchPosition(2);
                    return UpdateEmbedAsync();

                case Four:
                    if (this._selectionStates.Count() < 4) return Task.CompletedTask;
                    SwitchPosition(3);
                    return UpdateEmbedAsync();

                case Five:
                    if (this._selectionStates.Count() < 5) return Task.CompletedTask;
                    SwitchPosition(4);
                    return UpdateEmbedAsync();

                case Delete:
                    if (this._selectionStates.Count(x => x.Value.State) == 0) return Task.CompletedTask;
                    return DeleteSelectedAsync();

                case Reload:
                    return UpdateEmbedAsync();

                case Stop:
                    return CleanupAsync();
            }

            return Task.CompletedTask;
        }
        // End Implementation Methods

        // Message Updating

        /// <summary>
        /// Generates the embed
        /// </summary>
        private Embed GenerateEmbed()
        {
            var title = new StringBuilder("Active Reminders");

            if (!this._modRequested)
                title.Insert(0, "Your ");
            else
                title.Append($" of {this._queryUser}");

            var embed = new EmbedBuilder
            {
                Title = title.ToString(),
                Color = this._requestUser.GetRoleColor()
            };

            if (this._selectionStates.Count() == 0)
            {
                embed.Description = $"{(this._modRequested ? $"{this._queryUser} has" : "You have")} no active reminders!";
                return embed.Build();
            }

            foreach (var entry in this._selectionStates.ToList())
            {
                int index = entry.Key;
                var (reminder, selected) = entry.Value;

                var value = new StringBuilder($"Reminder {index+1}");

                if (selected)
                    value.Insert(0, Delete);

                embed.AddField(value.ToString(), reminder.ToString());
            }

            return embed.Build();
        }

        private async Task UpdateEmbedAsync()
        {
            UpdateCache(this._queryUser, this._guild); // Update cache
            this._embed = GenerateEmbed(); // Generate embed with updated cache
            await this._message.ModifyAsync(x => x.Embed = this._embed); // show new embed
        }

        /// <summary>
        /// Updates cache of selection states.
        /// <param name="user">
        ///     The user whose reminders to query.
        /// </param>
        /// <param name="guild">
        ///     The guild of the user's reminders.
        /// </param>
        /// </summary>
        private void UpdateCache(IUser user, IGuild guild)
        {
            var reminders = this._remindService.GetReminders(user, guild);

            var stateCount = this._selectionStates.Count();

            for (int i = 0; i < Math.Max(stateCount, reminders.Count()); i++)
            {
                var current = reminders.ElementAtOrDefault(i);

                if (current is null) // Remove indexes where no reminders exist anymore
                {
                    this._selectionStates.Remove(i, out _);
                    continue;
                }

                this._selectionStates.AddOrUpdate(i, (current, false), (position, oldState) => 
                {
                    if (oldState.Reminder?.Id != current.Id) // if the reminder doesn't match, then replace the state
                        return (current, false);
                    else
                        return oldState; // if they match, keep old state
                });
            }
        }
        // End Message Updating

        // Cache Updating

        /// <summary>
        ///     Deletes all selected reminders
        /// </summary>
        private async Task DeleteSelectedAsync()
        {
            foreach (var entry in this._selectionStates.ToList())
            {
                if (entry.Value.State)
                    this._remindService.DeleteReminder(entry.Value.Reminder.Id);
            }

            await UpdateEmbedAsync(); // Get new embed and send it
        }

        // Init

        /// <summary>
        /// Adds all required reactions to a message
        /// <param name="message">
        ///     The message to which to add the reactions
        /// </param>
        /// </summary>
        private static async Task AddReactionsAsync(IUserMessage message)
        {
            await message.AddReactionAsync(new Emoji(One));
            await message.AddReactionAsync(new Emoji(Two));
            await message.AddReactionAsync(new Emoji(Three));
            await message.AddReactionAsync(new Emoji(Four));
            await message.AddReactionAsync(new Emoji(Five));
            await message.AddReactionAsync(new Emoji(Delete));
            await message.AddReactionAsync(new Emoji(Stop));
            await message.AddReactionAsync(new Emoji(Reload));
        }

        /// <summary>
        /// This will set up all the triggers for this message
        /// </summary>
        private IEnumerable<InteractiveMessageTrigger> ConfigureTriggers()
        {
            yield return new InteractiveMessageTrigger(One, new UserTriggerCondition(this._requestUser),
                                                            new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Two, new UserTriggerCondition(this._requestUser),
                                                            new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Three, new UserTriggerCondition(this._requestUser),
                                                              new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Four, new UserTriggerCondition(this._requestUser),
                                                             new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Five, new UserTriggerCondition(this._requestUser),
                                                             new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Delete, new UserTriggerCondition(this._requestUser),
                                                               new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Stop, new UserTriggerCondition(this._requestUser),
                                                             new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Reload, new UserTriggerCondition(this._requestUser),
                                                             new MessageTriggerCondition(this._message));
        }
        // End Init

        // Various Helpers

        /// <summary>
        /// Switches the selected state.
        /// <param name="position">
        ///     The index of the state.
        /// </param>
        /// <param name="oldState">
        ///     The content of the state.
        /// </param>
        /// <returns>New content with switched selected value.</returns>
        /// </summary>
        private (Reminder Reminder, bool State) BoolSwitchFactory(int position, (Reminder Reminder, bool State) oldState)
            => (oldState.Reminder, !oldState.State);

        /// <summary>
        /// Switches the state at a specific postion
        /// </summary>
        private void SwitchPosition(int pos)
            => this._selectionStates.AddOrUpdate(pos, (null, false), BoolSwitchFactory);

        /// <summary>
        ///     Cleans up
        /// </summary>
        private async Task CleanupAsync()
        {
            await this._exited?.Invoke(this);
            if (this._modRequested) // If a mod requested, don't keep that message
                await this._message.DeleteAsync();
            await this._message.RemoveAllReactionsAsync();
        }
        // End Various Helpers
    }
}