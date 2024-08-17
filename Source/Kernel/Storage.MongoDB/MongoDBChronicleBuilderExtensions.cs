// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Reminders;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use MongoDB.
/// </summary>
public static class MongoDBChronicleBuilderExtensions
{
    static bool _mongoDBArtifactsInitializes;

    /// <summary>
    /// Configure Chronicle to use MongoDB.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithMongoDB(this IChronicleBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IMongoDBClientManager, MongoDBClientManager>();
            services.AddSingleton<IEncryptionKeyStorage, EncryptionKeyStorage>();
            services.AddSingleton<IStorage, Storage.MongoDB.Storage>();
        });

        if (!_mongoDBArtifactsInitializes)
        {
            BsonSerializer.TryRegisterSerializer(new JsonElementSerializer());
            BsonSerializer.TryRegisterSerializer(new UriSerializer());
            BsonSerializer.TryRegisterSerializer(new ConstraintDefinitionSerializer());
            _mongoDBArtifactsInitializes = true;
        }

        builder.ConfigureServices(services => services.AddSingleton<IReminderTable, ReminderTable>());
        return builder;
    }
}
