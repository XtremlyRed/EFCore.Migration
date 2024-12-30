using Microsoft.EntityFrameworkCore;

namespace EFCore.Migration.Models;

internal record KeepInfo<TDbContext>(TDbContext DbContext, SnapshotCodeInfo snapshotCodeInfo)
    : IDisposable
    where TDbContext : DbContext, IMigrateContext
{
    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
