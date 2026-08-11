// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IMaterializedReadModelStore"/> that reads instances from the
/// <see cref="ISink"/> a read model's observer writes to.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to resolve sinks through.</param>
/// <param name="compliance">The <see cref="IReadModelsCompliance"/> for releasing PII before instances leave the kernel.</param>
public class MaterializedReadModelStore(
    IStorage storage,
    IReadModelsCompliance compliance) : IMaterializedReadModelStore
{
    /// <summary>
    /// The number of instances read from the sink per round-trip when reading all of them.
    /// </summary>
    const int PageSize = 100;

    /// <inheritdoc/>
    /// <remarks>
    /// A passive read model registers with <see cref="SinkTypeId.None"/> — no observer ever writes to it, so
    /// its sink resolves to a <see cref="NullSink"/> that would answer every read with nothing.
    /// </remarks>
    public bool IsMaterialized(ReadModelDefinition definition) => definition.Sink.Type != SinkTypeId.None;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindByKey(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        Key key)
    {
        var sink = await GetSinkFor(eventStore, eventStoreNamespace, definition);
        var instance = await sink.FindOrDefault(key);
        if (instance is null)
        {
            return null;
        }

        return await compliance.Release(
            eventStore,
            eventStoreNamespace,
            definition.GetSchemaForLatestGeneration(),
            instance);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExpandoObject>> GetAllInstances(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition)
    {
        var sink = await GetSinkFor(eventStore, eventStoreNamespace, definition);
        var schema = definition.GetSchemaForLatestGeneration();
        var instances = new List<ExpandoObject>();
        var skip = 0;

        while (true)
        {
            var page = await sink.GetInstances(skip: skip, take: PageSize);
            var pageInstances = page.Instances.ToList();
            if (pageInstances.Count == 0)
            {
                break;
            }

            instances.AddRange(await compliance.Release(eventStore, eventStoreNamespace, schema, pageInstances));
            skip += pageInstances.Count;

            if (skip >= page.TotalCount)
            {
                break;
            }
        }

        return instances;
    }

    Task<ISink> GetSinkFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, ReadModelDefinition definition) =>
        storage.GetEventStore(eventStore).GetNamespace(eventStoreNamespace).Sinks.GetFor(definition);
}
