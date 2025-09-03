// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Factory for creating instances of <see cref="DbContext"/> at design time, typically when using EF migrations.
/// </summary>
/// <typeparam name="TDbContext">Type of <see cref="DbContext"/>.</typeparam>
public class DbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc/>
    public TDbContext CreateDbContext(string[] args)
    {
        var provider = GetArg(args, "--provider")
            ?? Environment.GetEnvironmentVariable("EF_PROVIDER")
            ?? "sqlserver";

        var builder = new DbContextOptionsBuilder<TDbContext>();
        switch (provider.ToLowerInvariant())
        {
            case "sqlserver":
                builder.UseSqlServer("Server=(local);Database=Dummy;Trusted_Connection=True;", x => x.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));
                break;
            case "npgsql":
            case "postgresql":
                builder.UseNpgsql("Host=localhost;Database=dummy;Username=x;Password=y", x => x.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));
                break;
            case "sqlite":
                builder.UseSqlite("Data Source=:memory:", x => x.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));
                break;
            default:
                throw new InvalidOperationException($"Unknown provider '{provider}'.");
        }
        builder.UseSqlite("Data Source=chronicle.db", x => x.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));
        return (Activator.CreateInstance(typeof(TDbContext), builder.Options) as TDbContext)!;
    }
    static string? GetArg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
