// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.Cluster;
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
        builder.SiloBuilder.AddStartupTask<ClusterDbContextMigrator>(ServiceLifecycleStage.First);

        builder.Services.AddSingleton<IDatabase, Database>();
        builder.Services.AddSingleton<IClusterStorage, ClusterStorage>();
        builder.Services.AddSingleton<IEncryptionKeyStorage, InMemoryEncryptionKeyStorage>();
        builder.Services.AddDbContextFactory<ClusterDbContext>((serviceProvider, optionsBuilder) =>
        {
            optionsBuilder
                .UseDatabaseFromConnectionString(options.Storage.ConnectionDetails)
                .UseApplicationServiceProvider(serviceProvider);
        });

        builder.Services.AddSingleton<IReminderTable, ReminderTable>();
        return builder;
    }
}
