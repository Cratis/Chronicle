// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use SQL.
/// </summary>
public static class SqlChronicleBuilderExtensions
{
    /// <summary>
    /// Configure Chronicle to use SQL, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithSql(this IChronicleBuilder builder, ChronicleOptions options)
    {
        builder.Services.AddSingleton<IClusterStorage, ClusterStorage>();
        builder.Services.AddDbContexts(options);
        builder.Services.AddSingleton<IReminderTable, ReminderTable>();
        builder.Services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>, MigrationStartupTask>();

        return builder;
    }

    static void AddDbContexts(this IServiceCollection services, ChronicleOptions options)
    {
        var addDbContextMethod = typeof(SqlChronicleBuilderExtensions).GetMethod(nameof(AddDbContext), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        foreach (var dbContext in Types.Types.Instance.FindMultiple<DbContext>().OnlyChronicle())
        {
            addDbContextMethod.MakeGenericMethod(dbContext).Invoke(null, [services, options]);
        }
    }

    static IServiceCollection AddDbContext<TDbContext>(this IServiceCollection services, ChronicleOptions options)
        where TDbContext : BaseDbContext
    {
        services.AddDbContext<TDbContext>(builder =>
        {
            switch (options.Storage.Type.ToLowerInvariant())
            {
                case StorageType.Sqlite:
                    builder.UseSqlite(options.Storage.ConnectionDetails);
                    break;
                case StorageType.SqlServer:
                    builder.UseSqlServer(options.Storage.ConnectionDetails);
                    break;
                case StorageType.PostgreSql:
                    builder.UseNpgsql(options.Storage.ConnectionDetails);
                    break;
                default:
                    throw new NotSupportedException($"Storage type '{options.Storage.Type}' is not supported.");
            }
        });

        return services;
    }
}
