// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Verifies MongoDB transaction support before starting a generation-fenced kernel.
/// </summary>
/// <param name="options">The Chronicle configuration.</param>
/// <param name="services">The provider resolving the active MongoDB backend.</param>
public class VerifyFencedMongoTopology(IOptions<ChronicleOptions> options, IServiceProvider services) : IStartupTask
{
    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        if (!options.Value.Features.ProjectionGenerations ||
            options.Value.Storage.Type.Equals(StorageType.Sqlite, StringComparison.OrdinalIgnoreCase) ||
            options.Value.Storage.Type.Equals(StorageType.MsSql, StringComparison.OrdinalIgnoreCase) ||
            options.Value.Storage.Type.Equals(StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        var database = services.GetRequiredService<IDatabase>();

        // Check the default destination even before the first event store has been persisted.
        await MongoTopology.RequireTransactions(database.GetReadModelDatabase(EventStoreName.System, EventStoreNamespaceName.Default), cancellationToken);
        using var stores = await database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores).FindAsync(FilterDefinition<EventStore>.Empty, cancellationToken: cancellationToken);
        foreach (var store in await stores.ToListAsync(cancellationToken))
        {
            var namespaces = database.GetEventStoreDatabase(store.Name).GetCollection<MongoDBNamespace>(WellKnownCollectionNames.Namespaces);
            using var cursor = await namespaces.FindAsync(FilterDefinition<MongoDBNamespace>.Empty, cancellationToken: cancellationToken);
            foreach (var @namespace in await cursor.ToListAsync(cancellationToken))
            {
                await MongoTopology.RequireTransactions(database.GetReadModelDatabase(store.Name, @namespace.Name), cancellationToken);
            }
        }
    }
}
