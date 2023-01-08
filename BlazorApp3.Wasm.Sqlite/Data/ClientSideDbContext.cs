using BlazorApp3.Wasm.Sqlite.Entities;

using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Wasm.Sqlite.Data
{
    public class ClientSideDbContext : DbContext
    {
        public DbSet<Person> Persons => Set<Person>();

        public ClientSideDbContext(DbContextOptions<ClientSideDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Person>().HasKey(x => x.Id);
            modelBuilder.Entity<Person>().Property(x => x.Name).UseCollation("nocase");
        }
    }
}
