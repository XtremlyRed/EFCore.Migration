# EFCore.Migration
EFCore.Migration

>  *IAutoMigrateContext*

>  *context.AutoMigrateAsync()*;


``` csharp
public class ProgramDbContext : DbContext, IAutoMigrateContext
{
    public ProgramDbContext(DbContextOptions<ProgramDbContext> options)
        : base(options) { }

    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProgramDbContext>(options =>
            options.UseSqlite(
                $"Data Source={Path.Combine(Environment.CurrentDirectory, $"sqlite1.db")}"
            )
        );
        var provider = services.BuildServiceProvider();

        using var context = provider.GetRequiredService<ProgramDbContext>();

        await context.AutoMigrateAsync();

        Console.ReadLine();
    }

    public DbSet<TestModel> TestModels { get; set; }
    public DbSet<TestModel2> TestModel2s { get; set; }

    public DbSet<MigrationEntity> MigrationEntities { get; set; }
}

public class TestModel
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime222333 { get; set; } = DateTime.Now;
    public DateTime S1 { get; set; } = DateTime.Now;
    public DateTime S2 { get; set; } = DateTime.Now;
    public DateTime S4 { get; set; } = DateTime.Now;
}

public class TestModel2
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
}
```
