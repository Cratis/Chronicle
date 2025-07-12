// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SQL.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.SQL;

/// <summary>
/// Extension methods for configuring SQL storage.
/// </summary>
public static class SqlStorageBuilderExtensions
{
    /// <summary>
    /// Add SQL storage support to Chronicle.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure SQL storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlStorage(
        this IServiceCollection services,
        Action<SqlStorageOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddDbContextFactory<SinkDbContext>((serviceProvider, options) =>
        {
            var sqlOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlStorageOptions>>().Value;

            switch (sqlOptions.ProviderType)
            {
                case SqlProviderType.SqlServer:
                    options.UseSqlServer(sqlOptions.ConnectionString);
                    break;

                case SqlProviderType.PostgreSQL:
                    options.UseNpgsql(sqlOptions.ConnectionString);
                    break;

                case SqlProviderType.SQLite:
                    options.UseSqlite(sqlOptions.ConnectionString);
                    break;

                default:
                    throw new NotSupportedException($"SQL provider type {sqlOptions.ProviderType} is not supported");
            }
        });

        services.AddTransient<SinkFactory>();

        return services;
    }

    /// <summary>
    /// Add SQL Server storage support to Chronicle.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">SQL Server connection string.</param>
    /// <param name="configureOptions">Action to configure additional SQL storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlServerStorage(
        this IServiceCollection services,
        string connectionString,
        Action<SqlStorageOptions>? configureOptions = null)
    {
        return services.AddSqlStorage(options =>
        {
            options.ProviderType = SqlProviderType.SqlServer;
            options.ConnectionString = connectionString;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// Add PostgreSQL storage support to Chronicle.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="configureOptions">Action to configure additional SQL storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlStorage(
        this IServiceCollection services,
        string connectionString,
        Action<SqlStorageOptions>? configureOptions = null)
    {
        return services.AddSqlStorage(options =>
        {
            options.ProviderType = SqlProviderType.PostgreSQL;
            options.ConnectionString = connectionString;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// Add SQLite storage support to Chronicle.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">SQLite connection string.</param>
    /// <param name="configureOptions">Action to configure additional SQL storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteStorage(
        this IServiceCollection services,
        string connectionString,
        Action<SqlStorageOptions>? configureOptions = null)
    {
        return services.AddSqlStorage(options =>
        {
            options.ProviderType = SqlProviderType.SQLite;
            options.ConnectionString = connectionString;
            configureOptions?.Invoke(options);
        });
    }
}
