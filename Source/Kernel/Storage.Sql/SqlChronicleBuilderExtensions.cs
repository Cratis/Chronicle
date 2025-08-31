// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sql;
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
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IClusterStorage, ClusterStorage>();
            services.AddDbContext<ClusterDbContext>(options);

            builder.Services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>, MigrationStartupTask>();
        });

        return builder;
    }

    static IServiceCollection AddDbContext<TDbContext>(this IServiceCollection services, ChronicleOptions options)
        where TDbContext : DbContext
    {
        services.AddDbContext<ClusterDbContext>(opts =>
        {
            switch (options.Storage.Type.ToLowerInvariant())
            {
                case StorageType.Sqlite:
                    opts.UseSqlite(options.Storage.ConnectionDetails);
                    break;
                case StorageType.SqlServer:
                    opts.UseSqlServer(options.Storage.ConnectionDetails);
                    break;
                case StorageType.PostgreSql:
                    opts.UseNpgsql(options.Storage.ConnectionDetails);
                    break;
                default:
                    throw new NotSupportedException($"Storage type '{options.Storage.Type}' is not supported.");
            }
        });

        return services;
    }
}
