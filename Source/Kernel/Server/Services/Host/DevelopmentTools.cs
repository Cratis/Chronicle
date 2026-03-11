// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.Runtime;
using KernelEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using KernelEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Services.Host;

/// <summary>
/// Represents an implementation of <see cref="IDevelopmentTools"/>.
/// </summary>
/// <remarks>Only available in Debug builds.</remarks>
/// <param name="mongoDBOptions"><see cref="MongoDBOptions"/> for resolving the MongoDB connection.</param>
/// <param name="clientManager"><see cref="IMongoDBClientManager"/> for obtaining a <see cref="IMongoClient"/>.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for interacting with Orleans grains.</param>
/// <param name="initializer"><see cref="IChronicleInitializer"/> for re-bootstrapping after a reset.</param>
/// <param name="storage"><see cref="IStorage"/> for managing event store registrations.</param>
internal sealed class DevelopmentTools(
    IOptions<MongoDBOptions> mongoDBOptions,
    IMongoDBClientManager clientManager,
    IGrainFactory grainFactory,
    IChronicleInitializer initializer,
    IStorage storage) : IDevelopmentTools
{
    static readonly string[] _systemDatabaseNames = ["admin", "config", "local"];

    /// <inheritdoc/>
    public async Task ResetAll()
    {
        var client = GetMongoClient();

        var namesCursor = await client.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();

        foreach (var name in names.Where(name => !_systemDatabaseNames.Contains(name)))
        {
            await client.DropDatabaseAsync(name);
        }

        await ForceGrainEviction();

        await initializer.Initialize();
    }

    /// <inheritdoc/>
    public async Task ResetEventStore(string eventStore)
    {
        KernelEventStoreName eventStoreName = eventStore;

        // Collect namespaces before dropping databases
        var namespacesGrain = grainFactory.GetGrain<INamespaces>(eventStoreName);
        var namespaces = (await namespacesGrain.GetAll()).ToArray();

        var client = GetMongoClient();

        // Drop event store metadata and per-namespace databases
        await client.DropDatabaseAsync($"{eventStore}+es");
        foreach (var ns in namespaces)
        {
            await client.DropDatabaseAsync($"{eventStore}+es+{ns}");
            var readModelDbName = ns == KernelEventStoreNamespaceName.Default ? eventStore : $"{eventStore}+{ns}";
            await client.DropDatabaseAsync(readModelDbName);
        }

        // Remove the event store from the registry so clients re-register it cleanly
        await storage.RemoveEventStore(eventStoreName);

        await ForceGrainEviction();
    }

    IMongoClient GetMongoClient()
    {
        var url = new MongoUrl(mongoDBOptions.Value.Server);
        var settings = MongoClientSettings.FromUrl(url);
        settings.DirectConnection = mongoDBOptions.Value.DirectConnection;
        return clientManager.GetClientFor(settings);
    }

    async Task ForceGrainEviction()
    {
        var managementGrain = grainFactory.GetGrain<IManagementGrain>(1);
        await managementGrain.ForceActivationCollection(TimeSpan.Zero);

        // Give grains a moment to finish deactivating before callers proceed
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}
#endif
