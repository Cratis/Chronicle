// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Serialization;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.Utils;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use MongoDB.
/// </summary>
public static class MongoDBChronicleBuilderExtensions
{
    /// <summary>
    /// Configure Chronicle to use MongoDB, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithMongoDB(this IChronicleBuilder builder, ChronicleOptions options) =>
        builder.WithMongoDB(options.Storage.ConnectionDetails, WellKnownDatabaseNames.Chronicle, options.Clustering.Type == ClusteringType.MongoDB);

    /// <summary>
    /// Configure Chronicle to use MongoDB.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="server">Connection string for the MongoDB server.</param>
    /// <param name="database">Name of the database to use. Defaults to the <see cref="WellKnownDatabaseNames.Chronicle"/>.</param>
    /// <param name="useClustering">Whether to also use MongoDB for Orleans cluster membership, letting multiple nodes form one cluster.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithMongoDB(this IChronicleBuilder builder, string server, string database = WellKnownDatabaseNames.Chronicle, bool useClustering = false)
    {
        var settings = GetMongoClientSettings(server);

        builder.ConfigureServices(services =>
        {
            var mongoClientDescriptor = services.LastOrDefault(_ => !_.IsKeyedService && _.ServiceType == typeof(IMongoClient));
            if (mongoClientDescriptor is null || mongoClientDescriptor.Lifetime != ServiceLifetime.Singleton)
            {
                // Orleans adds its default client and factory after this callback. Give it a dedicated client
                // unless an existing singleton IMongoClient is already safe for the default factory to consume.
                // This also covers Chronicle being configured before Arc adds its scoped resilience proxy.
                // Construct the Orleans factory from Chronicle's settings so it owns a singleton-safe client while
                // leaving Arc's IMongoClient registration and lifetime untouched. Never replace a consumer-provided
                // IMongoClientFactory.
                services.TryAddSingleton<IMongoClientFactory>(
                    _ => new DefaultMongoClientFactory(new MongoClient(settings)));
            }
        });

        builder.SiloBuilder.UseMongoDBClient(_ => settings);

        if (useClustering)
        {
            builder.SiloBuilder.UseMongoDBClustering(options =>
            {
                options.DatabaseName = database;
                options.Strategy = MongoDBMembershipStrategy.Multiple;
            });
        }

        builder.SiloBuilder.UseMongoDBReminders(options => options.DatabaseName = database);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ICustomSerializers, CustomSerializers>();
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IMongoDBClientManager, MongoDBClientManager>();
            services.AddSingleton<EncryptionKeyStorage>();
            services.AddSingleton<IEncryptionKeyStorage>(sp => new CacheEncryptionKeyStorage(sp.GetRequiredService<EncryptionKeyStorage>()));
            services.AddSingleton<IClusterStorage, ClusterStorage>();
            services.AddSingleton<ISystemStorage, SystemStorage>();
            services.AddSingleton<IStorage, Storage.Storage>();

            services.AddHealthChecks().AddMongoDb(
                _ => new MongoClient(settings),
                name: "mongodb",
                timeout: TimeSpan.FromSeconds(3));
        });

        return builder;
    }

    /// <summary>
    /// Create <see cref="MongoClientSettings"/> from a server connection string.
    /// </summary>
    /// <param name="server">Connection string for the MongoDB server.</param>
    /// <returns><see cref="MongoClientSettings"/> for the connection string.</returns>
    internal static MongoClientSettings GetMongoClientSettings(string server)
    {
        var url = new MongoUrl(server);
        return MongoClientSettings.FromUrl(url);
    }
}
