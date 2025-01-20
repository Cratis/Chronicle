// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use MongoDB.
/// </summary>
public static class MongoDBChronicleBuilderExtensions
{
    static bool _mongoDBArtifactsInitialized;

    /// <summary>
    /// Configure Chronicle to use MongoDB.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="server">Connection string for the MongoDB server.</param>
    /// <param name="database">Name of the database to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithMongoDB(this IChronicleBuilder builder, string server, string database)
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

        if (!_mongoDBArtifactsInitialized)
        {
            BsonSerializer.TryRegisterSerializer(new JsonElementSerializer());
            BsonSerializer.TryRegisterSerializer(new UriSerializer());
            BsonSerializer.TryRegisterSerializer(new ConstraintDefinitionSerializer());
            BsonSerializer.TryRegisterSerializer(new SiloAddressSerializer());
            _mongoDBArtifactsInitialized = true;
        }

        return builder;
    }
}
