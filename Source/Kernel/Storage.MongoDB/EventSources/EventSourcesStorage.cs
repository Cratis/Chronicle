// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSources;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSources;

/// <summary>
/// Represents the MongoDB implementation of <see cref="IEventSourcesStorage"/>.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/> to use.</param>
public class EventSourcesStorage(IEventStoreDatabase eventStoreDatabase) : IEventSourcesStorage
{
    IMongoCollection<EventSourceDefinition> Collection => eventStoreDatabase.GetCollection<EventSourceDefinition>(WellKnownCollectionNames.EventSources);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.EventSources.EventSourceDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<EventSourceDefinition>.Empty);
        var definitions = await result.ToListAsync();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.EventSources.EventSourceDefinition>> ObserveAll() =>
        new TransformingSubject<IEnumerable<EventSourceDefinition>, IEnumerable<Concepts.EventSources.EventSourceDefinition>>(
            Collection.Observe(),
            definitions => definitions.Select(definition => definition.ToKernel()));

    /// <inheritdoc/>
    public async Task<Concepts.EventSources.EventSourceDefinition?> Find(EventSourceName name)
    {
        var id = name.Value;
        using var result = await Collection.FindAsync(definition => definition.Id == id);
        var definition = await result.FirstOrDefaultAsync();
        return definition?.ToKernel();
    }

    /// <inheritdoc/>
    public Task Save(Concepts.EventSources.EventSourceDefinition definition) =>
        Collection.ReplaceOneAsync(
            filter: document => document.Id == definition.Name.Value,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
