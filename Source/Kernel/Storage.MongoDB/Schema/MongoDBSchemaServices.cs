// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Registers the services for offline MongoDB index operations, without a web host or Orleans.
/// </summary>
public static class MongoDBSchemaServices
{
    /// <summary>
    /// Adds index provisioning with the schema connection purpose.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="options">The Chronicle options.</param>
    public static void Add(IServiceCollection services, ChronicleOptions options)
    {
        services.AddStorageConnectionResolution(WellKnownConnectionStorage.MongoDB, options.Storage.ConnectionResolution);
        services.Configure<MongoDBOptions>(mongo =>
        {
            mongo.Server = options.Storage.ConnectionDetails;
            mongo.Database = DatabaseNames.WithPrefix(WellKnownDatabaseNames.Chronicle, options.Storage.DatabaseNamePrefix);
        });
        services.Configure<MongoDBStorageOptions>(mongo => mongo.DatabaseNamePrefix = options.Storage.DatabaseNamePrefix);
        services.AddSingleton<IMongoDBStorageConnections>(provider => new MongoDBStorageConnections(
            provider.GetRequiredService<IOptions<MongoDBOptions>>(),
            provider.GetRequiredService<IStorageConnectionResolution>(),
            provider.GetRequiredService<IMongoDBClientManager>(),
            ConnectionPurpose.Schema));
        services.AddSingleton<IDatabase, Database>();
        services.AddSingleton<IMongoDBIndexProvisioner, MongoDBIndexProvisioner>();
    }
}
