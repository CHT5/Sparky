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
        private const string One = "1⃣";

        private const string Two = "2⃣";

        private const string Three = "3⃣";

        private const string Four = "4⃣";

        private const string Five = "5⃣";

        private const string Delete = "❌";

        private const string Stop = "⏹";

        private const string Reload = "🔄";

        private readonly IUser _user;

        private readonly IGuild _guild;

        private readonly IMessageChannel _channel;

        IEnumerable<InteractiveMessageTrigger> IInteractiveMessage.Triggers => this._triggers;

        private List<InteractiveMessageTrigger> _triggers;

        private Embed _embed;

        private IUserMessage _message;

        private ConcurrentDictionary<int, (Reminder Reminder, bool State)> _selectionStates;

        private RemindService _remindService;

        public InteractiveReminderMessage(IUser user, ITextChannel channel)
        {
            this._triggers = new List<InteractiveMessageTrigger>();
            this._selectionStates = new ConcurrentDictionary<int, (Reminder Reminder, bool State)>();
            this._user = user;
            this._guild = channel.Guild;
            this._channel = channel;
        }

        async Task IInteractiveMessage.SetUpAsync()
        {
            this.UpdateCache(this._user, this._guild);
            this._embed = this.GenerateEmbed();
            this._message = await this._channel.SendMessageAsync("", embed: this._embed);
            this._triggers = ConfigureTriggers().ToList();
            await AddReactionsAsync(this._message);
        }

        void IInteractiveMessage.AddServiceProvider(IServiceProvider provider)
            => this._remindService = provider.GetService<RemindService>();

        private IEnumerable<InteractiveMessageTrigger> ConfigureTriggers()
        {
            yield return new InteractiveMessageTrigger(One, new UserTriggerCondition(this._user),
                                                            new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Two, new UserTriggerCondition(this._user),
                                                            new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Three, new UserTriggerCondition(this._user),
                                                              new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Four, new UserTriggerCondition(this._user),
                                                             new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Five, new UserTriggerCondition(this._user),
                                                             new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Delete, new UserTriggerCondition(this._user),
                                                               new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Stop, new UserTriggerCondition(this._user),
                                                             new MessageTriggerCondition(this._message));
            yield return new InteractiveMessageTrigger(Reload, new UserTriggerCondition(this._user),
                                                             new MessageTriggerCondition(this._message));
        }

        private void UpdateCache(IUser user, IGuild guild)
        {
            var reminders = this._remindService.GetReminders(user, guild);

            var stateCount = this._selectionStates.Count();

            for (int i = 0; i < Math.Max(stateCount, reminders.Count()); i++)
            {
                var current = reminders.ElementAtOrDefault(i);

                if (current is null)
                {
                    this._selectionStates.Remove(i, out _);
                    continue;
                }

                this._selectionStates.AddOrUpdate(i, (current, false), (position, oldState) => 
                {
                    if (oldState.Reminder?.Id != current.Id)
                        return (current, false);
                    else
                        return oldState;
                });
            }
        }

        private Embed GenerateEmbed()
        {
            var embed = new EmbedBuilder
            {
                Title = "Active Reminders",
                Color = this._user.GetRoleColor()
            };

            if (this._selectionStates.Count() == 0)
            {
                embed.Description = $"You have no active reminders!";
                return embed.Build();
            }

            foreach (var entry in this._selectionStates.ToList())
            {
                int index = entry.Key;
                (Reminder reminder, bool selected) = entry.Value;

                StringBuilder title = new StringBuilder($"Reminder {index+1}");

                if (selected)
                    title.Insert(0, Delete);

                embed.AddField(title.ToString(), reminder.ToString());
            }

            return embed.Build();
        }

        private async Task UpdateEmbedAsync()
        {
            UpdateCache(this._user, this._guild);
            this._embed = GenerateEmbed();
            await this._message.ModifyAsync(x => x.Embed = this._embed);
        }

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

        private async Task DeleteSelectedAsync()
        {
            foreach (var entry in this._selectionStates.ToList())
            {
                if (entry.Value.State)
                    this._remindService.DeleteReminder(entry.Value.Reminder.Id);
            }

            await UpdateEmbedAsync(); 
        }

        private (Reminder Reminder, bool State) BoolSwitchFactory(int position, (Reminder Reminder, bool State) oldState)
            => (oldState.Reminder, !oldState.State);

        private void SwitchPosition(int pos)
            => this._selectionStates.AddOrUpdate(pos, (null, false), BoolSwitchFactory);

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
                    return this._message.RemoveAllReactionsAsync();
            }

            return Task.CompletedTask;
        }
    }
}