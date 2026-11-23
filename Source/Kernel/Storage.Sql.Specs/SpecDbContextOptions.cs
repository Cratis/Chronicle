// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    /// <remarks>
    /// The kernel configures a distinct option set per context type and provider, which together exceed the twenty internal
    /// service providers EF Core tolerates in one process, so the warning about that is ignored; see <see cref="IgnoringManyServiceProviders{TBuilder}"/>.
    /// </remarks>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="connectionString">The connection string for the isolated specification database.</param>
    /// <returns>The kernel-shaped options for the context.</returns>
    public static DbContextOptions<TContext> Kernel<TContext>(string connectionString)
        where TContext : DbContext => new DbContextOptionsBuilder<TContext>(Database.BuildOptions<TContext>(_sharedServices, connectionString))
            .IgnoringManyServiceProviders()
            .Options;

    /// <summary>
    /// Builds shared kernel options for PostgreSQL, keeping the default options for other providers.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="connectionString">The connection string for the specification database.</param>
    /// <returns>The options for the context.</returns>
    public static DbContextOptions<TContext> ForConnection<TContext>(string connectionString)
        where TContext : DbContext => connectionString.GetDatabaseType() == DatabaseType.PostgreSql
            ? Kernel<TContext>(connectionString)
            : (DbContextOptions<TContext>)new DbContextOptionsBuilder<TContext>().UseDatabaseFromConnectionString(connectionString).IgnoringManyServiceProviders().Options;

    /// <summary>
    /// Disables EF Core's internal service provider cache for the options being built.
    /// </summary>
    /// <remarks>
    /// The specs build far more than twenty distinct option sets in one process, and EF Core throws
    /// <see cref="Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning"/> for any created after the twentieth, which makes the
    /// failing spec depend on test order. Every spec harness goes through this helper, which also ignores that warning
    /// (see <see cref="IgnoringManyServiceProviders{TBuilder}"/>) because it counts the providers cached by other specs.
    /// </remarks>
    /// <typeparam name="TBuilder">The type of options builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder for continuation.</returns>
    public static TBuilder WithoutServiceProviderCaching<TBuilder>(this TBuilder builder)
        where TBuilder : DbContextOptionsBuilder
    {
        builder.EnableServiceProviderCaching(false);
        return builder.IgnoringManyServiceProviders();
    }

    /// <summary>
    /// Stops EF Core from failing a context because the process already holds more than twenty internal service providers.
    /// </summary>
    /// <remarks>
    /// EF Core raises <see cref="CoreEventId.ManyServiceProvidersCreatedWarning"/> whenever it builds a provider while its cache
    /// holds more than twenty, including a provider that is not cached. The warning is therefore a property of the whole
    /// process, not of the options being built, and fails whichever spec builds a provider after the twentieth is cached.
    /// </remarks>
    /// <typeparam name="TBuilder">The type of options builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder for continuation.</returns>
    public static TBuilder IgnoringManyServiceProviders<TBuilder>(this TBuilder builder)
        where TBuilder : DbContextOptionsBuilder
    {
        builder.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        return builder;
    }
}
