// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis;
using Aksio.Cratis.Configuration;
using Aksio.Execution;
using Aksio.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring MongoDB based read models.
/// </summary>
public static class MongoDBReadModels
{
    static readonly ConcurrentDictionary<TenantId, IMongoDatabase> _databasesPerTenant = new();

    /// <summary>
    /// Add all services related to being able to use MongoDB for read models.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddMongoDBReadModels(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = default)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope - Logger factory will be disposed when process exits
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("MongodBReadModels");

        services.AddTransient(sp =>
        {
            var executionContext = sp.GetService<Aksio.Execution.ExecutionContext>()!;
            lock (_databasesPerTenant)
            {
                if (_databasesPerTenant.IsEmpty)
                {
                    ConfigureReadModels(sp).Wait();
                }
            }
            return _databasesPerTenant[executionContext.TenantId];
        });

        services.AddTransient(typeof(IMongoCollection<>), typeof(MongoCollectionAdapter<>));

        return services;
    }

    static async Task ConfigureReadModels(IServiceProvider serviceProvider)
    {
        // var storage = await serviceProvider.GetRequiredService<IMicroserviceConfiguration>().Storage();
        // var clientFactory = serviceProvider.GetRequiredService<IMongoDBClientFactory>();
        // foreach (var (tenant, config) in storage.Tenants)
        // {
        //     var storageType = config.Get(WellKnownStorageTypes.ReadModels);
        //     var url = new MongoUrl(storageType.ConnectionDetails.ToString()!);
        //     var client = clientFactory.Create(url);
        //     _databasesPerTenant[tenant] = client.GetDatabase(url.DatabaseName);
        // }

        await Task.CompletedTask;
    }
}
