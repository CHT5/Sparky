using System;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace CWSBot.Entities
{
    public class NumericTimestampGenerator : ValueGenerator
    {
        public override bool GeneratesTemporaryValues => false;

        protected override object NextValue(EntityEntry entry)
            => (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}