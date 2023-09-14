using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VinderenApi.Models;

namespace VinderenApi.DbContext
{
    public class EntityContext : IdentityDbContext
    {
        public EntityContext(DbContextOptions<EntityContext> options) 
            : base(options)
        {
            Taekwondo = Set<TaekwondoModel>();
            Kickboxing = Set<KickboxingModel>();
            Boxing = Set<BoxingModel>();
        }

        public DbSet<TaekwondoModel> Taekwondo { get; set; }
        public DbSet<KickboxingModel> Kickboxing { get; set;}
        public DbSet<BoxingModel> Boxing { get; set;}
    }
}
