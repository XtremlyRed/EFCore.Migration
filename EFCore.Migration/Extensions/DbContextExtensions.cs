using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFCore.Migration.Internals;
using EFCore.Migration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using static System.Reflection.BindingFlags;

namespace EFCore.Migration.Extensions;

internal static class DbContextExtensions
{
    /// <summary>
    /// get <see cref="DbContextOptions"/>
    /// </summary>
    /// <param name="otherContext"></param>
    /// <returns></returns>
    internal static MigrateContext CreateMigrateContext(this DbContext otherContext)
    {
        IDbContextOptions existOpts =
            typeof(DbContext).GetField("_options", NonPublic | Instance)?.GetValue(otherContext)
                as IDbContextOptions
            ?? otherContext.Database.GetService<IDbContextOptions>();

        DbContextOptionsBuilder<MigrateContext> optionsBuilder =
            new DbContextOptionsBuilder<MigrateContext>();

        DbContextOptions opts = optionsBuilder.Options;

        foreach (IDbContextOptionsExtension item in existOpts!.Extensions)
        {
            opts = opts.WithExtension(item);
        }

        return new MigrateContext(optionsBuilder.Options);
    }

    internal static SnapshotCodeInfo GetSnapshotCodeInfo(this DbContext dbContext)
    {
        var token = dbContext.GetType().FullName!.Replace(".", "_");

        var name_space = $"{token}.magration";
        var class_name = $"{dbContext.GetType().Name}_snapshot";

        var snapshot = new SnapshotCodeInfo(token, name_space, class_name);

        return snapshot;
    }
}
