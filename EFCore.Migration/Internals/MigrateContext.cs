using EFCore.Migration.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Migration.Internals;

internal class MigrateContext : DbContext
{
    public MigrateContext(DbContextOptions<MigrateContext> options)
        : base(options) { }

    public DbSet<MigrationEntity>? MigrationEntities { get; set; }
}
