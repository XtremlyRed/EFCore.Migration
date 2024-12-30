using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFCore.Migration.Extensions;
using EFCore.Migration.Internals;
using EFCore.Migration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using static System.Reflection.BindingFlags;

namespace EFCore.Migration;

/// <summary>
///
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// auto migrate
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public static async Task AutoMigrateAsync<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext, IMigrateContext
    {
        await Task.Run(() => AutoMigrate<TDbContext>(dbContext));
    }

    /// <summary>
    /// auto migrate
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <param name="dbContext"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void AutoMigrate<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext, IMigrateContext
    {
        if (dbContext.GetType().IsPublic == false)
        {
            throw new ArgumentException("non public database context");
        }

        var snapshotCodeInfo = dbContext.GetSnapshotCodeInfo();

        var snapshot = GetLatestSnapshot(dbContext, snapshotCodeInfo);

        var differences2 = dbContext.GetDifferences(snapshot);

        dbContext.MigrateDifferences(differences2);

        KeepSnapshotBuffer(dbContext, snapshotCodeInfo);
    }

    internal static IReadOnlyList<MigrationOperation> GetDifferences(
        this DbContext context,
        ModelSnapshot? modelSnapshot = null
    )
    {
        IMigrationsModelDiffer modelDiffer = context.Database.GetService<IMigrationsModelDiffer>();

#if NET6_0_OR_GREATER

        IRelationalModel codeModel = context.InitializeMode(modelSnapshot?.Model);
        IRelationalModel finaMode = context.GetRelationalModel();

#elif NETSTANDARD2_0

        IModel codeModel = modelSnapshot?.Model!;
        IModel finaMode = context.Model;

#endif

        IReadOnlyList<MigrationOperation> opts = null!;

        // has changed
        if (modelDiffer.HasDifferences(codeModel, finaMode) == false)
        {
            return opts;
        }

        //get migration operation
        opts = modelDiffer.GetDifferences(codeModel, finaMode);

        return opts;
    }

#if NET6_0_OR_GREATER

    internal static IRelationalModel InitializeMode(this DbContext context, IModel? model)
    {
        if (model is null)
        {
            return null!;
        }

        IModelRuntimeInitializer init = context.Database.GetService<IModelRuntimeInitializer>();

        IModel codeModel = init.Initialize(model, true);

        IRelationalModel finaMode = codeModel.GetRelationalModel();

        return finaMode;
    }

    internal static IRelationalModel GetRelationalModel(this DbContext context)
    {
        var designTimeModel = context.GetService<IDesignTimeModel>();

        var model = designTimeModel.Model;

        IRelationalModel finaMode = model.GetRelationalModel();

        return finaMode;
    }
#endif

    internal static int MigrateDifferences(
        this DbContext context,
        IReadOnlyList<MigrationOperation> operations
    )
    {
        if (operations is null || operations.Count == 0)
        {
            return -1;
        }

        //migrate column name
        List<string> allCommandTexts = new();

        //other migrate
        if (operations.Count > 0)
        {
#if NET6_0_OR_GREATER

            IModel mode = context.Database.GetService<IDesignTimeModel>().Model;

#elif NETSTANDARD2_0

            IModel mode = context.Model;
#endif
            //generate sql scripts
            string[] commandTexts = context
                .Database.GetService<IMigrationsSqlGenerator>()
                .Generate(operations, mode)
                .Select(p => p.CommandText)
                .ToArray();

            allCommandTexts.AddRange(commandTexts);
        }

        int changeCount = 0;

        for (int i = 0, length = allCommandTexts?.Count ?? 0; i < length; i++)
        {
            try
            {
                changeCount += context.Database.ExecuteSqlRaw(allCommandTexts![i]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        return changeCount;
    }

    internal static ModelSnapshot GetLatestSnapshot<TDbContext>(
        this TDbContext dbContext,
        SnapshotCodeInfo snapshotCodeInfo
    )
        where TDbContext : DbContext, IMigrateContext
    {
        var fullName = dbContext.GetType().FullName!.Replace(".", "_");

        MigrationEntity? exist = default;

        try
        {
            exist = dbContext
                .MigrationEntities!.Where(i => i.Name == fullName)
                .OrderByDescending(i => i.MigrationTime)
                .FirstOrDefault();
        }
        catch (Exception)
        {
            return default!;
        }

        if (exist is null)
        {
            return default!;
        }

        var buffer = CompressHelper.Decompress(exist.Migrations!);

        Assembly assembly = Assembly.Load(buffer!);

        if (assembly.CreateInstance(snapshotCodeInfo.TypeName) is ModelSnapshot modelSnapshot)
        {
            return modelSnapshot;
        }

        return default!;
    }

    internal static void KeepSnapshotBuffer<TDbContext>(
        this TDbContext context,
        SnapshotCodeInfo snapshotCodeInfo
    )
        where TDbContext : DbContext, IMigrateContext
    {
        var syncContext = context.Database.GetService<TDbContext>();

        var keep = new KeepInfo<TDbContext>(syncContext, snapshotCodeInfo);

        ThreadPool.QueueUserWorkItem(
            static t =>
            {
                using var keep = (KeepInfo<TDbContext>)t!;

#if NET6_0_OR_GREATER

                IModel mode = keep.DbContext.Database.GetService<IDesignTimeModel>().Model;

#elif NETSTANDARD2_0

                IModel mode = keep.DbContext.Model;

#endif
                DesignTimeServicesBuilder builder = new DesignTimeServicesBuilder(
                    keep.DbContext.GetType().Assembly!,
                    Assembly.GetEntryAssembly()!,
                    new OperationReporter(new OperationReportHandler()),
                    new string[0]
                );

                string code = builder
                    .Build(keep.DbContext)
                    .GetService<IMigrationsCodeGenerator>()!
                    .GenerateSnapshot(
                        keep.snapshotCodeInfo.Namespace,
                        keep.DbContext.GetType(),
                        keep.snapshotCodeInfo.ClassName,
                        mode
                    );

                var buffer = RoslynCompile.Compile(code);

                var bf2 = CompressHelper.Compress(buffer);

                var auto = new MigrationEntity(bf2, keep.snapshotCodeInfo.Token);

                keep.DbContext.MigrationEntities.Add(auto);

                keep.DbContext.SaveChanges();
            },
            keep
        );
    }
}
