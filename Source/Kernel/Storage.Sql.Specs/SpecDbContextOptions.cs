// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Extensions for building <see cref="DbContextOptions"/> in the specs.
/// </summary>
public static class SpecDbContextOptions
{
    static readonly ServiceProvider _sharedServices = new ServiceCollection().BuildServiceProvider();

    /// <summary>
    /// Builds options with the kernel's EF services and internal service provider caching enabled.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="connectionString">The connection string for the isolated specification database.</param>
    /// <returns>The kernel-shaped options for the context.</returns>
    public static DbContextOptions<TContext> Kernel<TContext>(string connectionString)
        where TContext : DbContext => Database.BuildOptions<TContext>(_sharedServices, connectionString);

    /// <summary>
    /// Builds shared kernel options for PostgreSQL, keeping the default options for other providers.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="connectionString">The connection string for the specification database.</param>
    /// <returns>The options for the context.</returns>
    public static DbContextOptions<TContext> ForConnection<TContext>(string connectionString)
        where TContext : DbContext => connectionString.GetDatabaseType() == DatabaseType.PostgreSql
            ? Kernel<TContext>(connectionString)
            : new DbContextOptionsBuilder<TContext>().UseDatabaseFromConnectionString(connectionString).Options;

    /// <summary>
    /// Disables EF Core's internal service provider cache for the options being built.
    /// </summary>
    /// <remarks>
    /// The specs build far more than twenty distinct option sets in one process, and EF Core throws
    /// <see cref="Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning"/> for any created after the twentieth, which makes the
    /// failing spec depend on test order. Every spec harness goes through this helper.
    /// </remarks>
    /// <typeparam name="TBuilder">The type of options builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder for continuation.</returns>
    public static TBuilder WithoutServiceProviderCaching<TBuilder>(this TBuilder builder)
        where TBuilder : DbContextOptionsBuilder
    {
        builder.EnableServiceProviderCaching(false);
        return builder;
    }
}
