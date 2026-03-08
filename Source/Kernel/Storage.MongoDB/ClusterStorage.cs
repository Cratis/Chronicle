// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Configuration;
using Cratis.Reactive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IClusterStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> instance.</param>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> instance.</param>
/// <param name="expandoObjectConverter">The <see cref="Json.IExpandoObjectConverter"/> instance.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> instance.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> instance.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> instance.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance.</param>
public class ClusterStorage(
    IDatabase database,
    IJsonComplianceManager complianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    IJobTypes jobTypes,
    IOptions<ChronicleOptions> options,
    ILoggerFactory loggerFactory) : IClusterStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores()
    {
        var collection = GetCollection();
        using var result = await collection.FindAsync(_ => true);
        return result.ToList().Select(_ => _.Name);
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores()
    {
        var collection = GetCollection();
        return new TransformingSubject<IEnumerable<EventStore>, IEnumerable<EventStoreName>>(
            collection.Observe(),
            _ => _.Select(_ => _.Name));
    }

    /// <inheritdoc/>
    public IEventStoreStorage CreateStorageForEventStore(EventStoreName eventStore, SinksFactory sinksFactory) => new EventStoreStorage(
        eventStore,
        database.GetEventStoreDatabase(eventStore),
        complianceManager,
        expandoObjectConverter,
        jsonSerializerOptions,
        sinksFactory,
        jobTypes,
        options,
        loggerFactory);

    /// <inheritdoc/>
    public async Task SaveEventStore(EventStoreName eventStore)
    {
        var collection = GetCollection();
        await collection.ReplaceOneAsync(_ => _.Name == eventStore, new EventStore(eventStore), new ReplaceOptions { IsUpsert = true });
    }

    IMongoCollection<EventStore> GetCollection() => database.GetCollection<EventStore>(WellKnownCollectionNames.EventStores);
}
