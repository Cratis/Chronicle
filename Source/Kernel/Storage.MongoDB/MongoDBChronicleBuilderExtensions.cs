// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleCoreBuilder"/> for configuring Chronicle to use MongoDB.
/// </summary>
public static class MongoDBChronicleBuilderExtensions
{
    /// <summary>
    /// Configure Chronicle to use MongoDB, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleCoreBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleCoreBuilder"/> for continuation.</returns>
    public static IChronicleCoreBuilder WithMongoDB(this IChronicleCoreBuilder builder, ChronicleOptions options) =>
        builder.WithMongoDB(options.Storage.ConnectionDetails, WellKnownDatabaseNames.Chronicle);

    /// <summary>
    /// Configure Chronicle to use MongoDB.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleCoreBuilder"/> to configure.</param>
    /// <param name="server">Connection string for the MongoDB server.</param>
    /// <param name="database">Name of the database to use. Defaults to the <see cref="WellKnownDatabaseNames.Chronicle"/>.</param>
    /// <returns><see cref="IChronicleCoreBuilder"/> for continuation.</returns>
    public static IChronicleCoreBuilder WithMongoDB(this IChronicleCoreBuilder builder, string server, string database = WellKnownDatabaseNames.Chronicle)
    {
        builder.SiloBuilder
            .UseMongoDBClient(server)

            // .UseMongoDBClustering(options =>
            // {
            //     options.DatabaseName = database;
            //     options.Strategy = MongoDBMembershipStrategy.Multiple;
            // })
            .UseMongoDBReminders(options => options.DatabaseName = database);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IMongoDBClientManager, MongoDBClientManager>();
            services.AddSingleton<IEncryptionKeyStorage, EncryptionKeyStorage>();
            services.AddSingleton<IStorage, Storage.MongoDB.Storage>();
        });

        return builder;
    }
}
