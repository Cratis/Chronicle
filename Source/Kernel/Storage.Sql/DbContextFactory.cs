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
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseSqlite("Data Source=Chronicle.db", x => x.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));
        return (Activator.CreateInstance(typeof(TDbContext), builder.Options) as TDbContext)!;
    }
}
