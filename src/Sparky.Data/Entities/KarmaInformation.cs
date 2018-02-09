using System;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class KarmaInformation
    {
        private readonly KarmaContext _context;

        private readonly KarmaUserModel _model;

        public ulong UserId { get; }

        public uint KarmaCount { get; }

        public DateTimeOffset NextKarmaAt { get; }

        public DateTimeOffset? LastKarmaGivenAt { get; }

        public bool CanGiveKarma => NextKarmaAt < DateTimeOffset.Now;

        internal KarmaInformation(KarmaContext context, KarmaUserModel model)
        {
            this._context = context;
            this._model = model;
            this.UserId = model.UserId;
            this.KarmaCount = model.KarmaCount;
            this.NextKarmaAt = model.NextKarmaAt;
            this.LastKarmaGivenAt = model.LastKarmaGivenAt;
        }

        public KarmaInformation Modify(Action<KarmaProperties> properties)
        {
            var props =  new KarmaProperties(this);
            properties(props);

            if (props.NextKarmaAt.HasValue && props.NextKarmaIn.HasValue)
                throw new ArgumentException($"{nameof(props.NextKarmaAt)} and {nameof(props.NextKarmaIn)} can't both be set");

            if (props.KarmaCount.HasValue)
                this._model.KarmaCount = props.KarmaCount.Value;

            if (props.NextKarmaAt.HasValue)
                this._model.NextKarmaAt = props.NextKarmaAt.Value;

            if (props.NextKarmaIn.HasValue)
                this._model.NextKarmaAt = DateTimeOffset.UtcNow.Add(props.NextKarmaIn.Value);

            this._context.SaveChanges();

            return new KarmaInformation(this._context, this._model);
        }
    }

    public class KarmaProperties
    {
        public uint? KarmaCount { get; set; }

        public DateTimeOffset? NextKarmaAt { get; set; }

        public DateTimeOffset? LastKarmaGivenAt { get; set; }

        public TimeSpan? NextKarmaIn { get; set; }

        internal KarmaProperties(KarmaInformation information)
            => this.KarmaCount = information.KarmaCount;
    }
}