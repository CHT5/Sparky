using Microsoft.EntityFrameworkCore;

namespace Sparky.Data.Models
{
    internal abstract class BaseModel
    {
        public abstract void ConfigureModel(ModelBuilder builder);
    }
}