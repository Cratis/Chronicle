// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Compliance.MongoDB;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.InProcess;

/// <summary>
/// Extension methods for <see cref="IChronicleInProcessBuilder"/> for configuring Chronicle to use MongoDB.
/// </summary>
public static class MongoDBChronicleInProcessBuilderExtensions
{
    /// <summary>
    /// Configure Chronicle to use MongoDB.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleInProcessBuilder"/> to configure.</param>
    /// <param name="server">Connection string for the MongoDB server.</param>
    /// <param name="database">Name of the database to use. Defaults to <c>chronicle+main</c>.</param>
    /// <returns><see cref="IChronicleInProcessBuilder"/> for continuation.</returns>
    public static IChronicleInProcessBuilder WithMongoDB(this IChronicleInProcessBuilder builder, string server, string database = WellKnownDatabaseNames.Chronicle)
    {
        builder.SiloBuilder
            .UseMongoDBClient(server)
            .UseMongoDBReminders(options => options.DatabaseName = database);

        builder.Services.AddSingleton<IDatabase, Database>();
        builder.Services.AddSingleton<IMongoDBClientManager, MongoDBClientManager>();
        builder.Services.AddSingleton<IEncryptionKeyStorage, EncryptionKeyStorage>();
        builder.Services.AddSingleton<IStorage, Storage.MongoDB.Storage>();

        return builder;
    }
}
