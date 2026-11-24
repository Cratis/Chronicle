// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Stages a complete storage-wide inventory before committing any database's completion.
/// </summary>
/// <param name="storage">The authoritative storage.</param>
internal sealed class ReadModelProvenance(IStorage storage)
{
    readonly Dictionary<SinkProvenanceDatabaseId, Destination> _destinations = [];
    readonly List<Source> _sources = [];

    internal async Task Inventory(EventStoreName requestedStore, EventStoreNamespaceName? requestedNamespace, ReadModelDefinition? requestedModel)
    {
        var stores = (await storage.GetEventStores()).Append(requestedStore).Distinct(CaseInsensitiveEventStoreNameComparer.Instance).ToArray();
        foreach (var store in stores)
        {
            await Discover(store);
        }

        if (requestedModel is not null && requestedNamespace is not null)
        {
            await AddDestination(storage.GetEventStore(requestedStore), requestedNamespace, requestedModel);
        }

        await GatherEvidence();

        // No completion is published until discovery and evidence collection for every scope has succeeded.
        foreach (var destination in _destinations.Values.Where(_ => !_.State.IsComplete))
        {
            await destination.Storage.CompleteInventory(destination.RestrictedEvidence);
        }
    }

    async Task GatherEvidence()
    {
        foreach (var source in _sources)
        {
            if (source.Destinations.All(_ => _.State.IsComplete)) continue;
            var restricted = (await LegacyReadModelSinkInventory.Find(source.Storage, includeUnresolved: true)).Select(_ => _.Identifier).ToHashSet();
            foreach (var binding in source.Bindings.Where(_ => restricted.Contains(_.Model.Identifier)))
            {
                binding.Destination.RestrictedEvidence = true;
            }
        }
    }

    async Task Discover(EventStoreName store)
    {
        var eventStore = storage.GetEventStore(store);
        var models = (await eventStore.ReadModels.GetAll()).ToArray();
        var namespaces = (await eventStore.Namespaces.GetAll()).Select(_ => _.Name)
            .Append(EventStoreNamespaceName.Default).Append(EventStoreNamespaceName.NotSet).Distinct().ToArray();
        var source = new Source(eventStore);
        _sources.Add(source);
        foreach (var model in models)
        {
            foreach (var @namespace in namespaces)
            {
                if (await AddDestination(eventStore, @namespace, model) is { } destination)
                {
                    source.Bindings.Add(new(model, destination));
                }
            }
        }
    }

    async Task<Destination?> AddDestination(IEventStoreStorage store, EventStoreNamespaceName @namespace, ReadModelDefinition model)
    {
        if (SinkProvenanceExemptions.IsNonDatabase(model.Sink.TypeId)) return null;
        var provenance = store.GetNamespace(@namespace).Sinks.GetProvenance(model);
        var state = (await provenance.ReadState()).Validate();
        if (!_destinations.TryGetValue(state.DatabaseId, out var destination))
        {
            destination = new(provenance, state);
            _destinations.Add(state.DatabaseId, destination);
        }

        return destination;
    }

    sealed class Source(IEventStoreStorage storage)
    {
        internal IEventStoreStorage Storage { get; } = storage;
        internal List<Binding> Bindings { get; } = [];
        internal IEnumerable<Destination> Destinations => Bindings.Select(_ => _.Destination);
    }

    sealed record Binding(ReadModelDefinition Model, Destination Destination);

    sealed class Destination(ISinkProvenanceStorage storage, SinkProvenanceState state)
    {
        internal ISinkProvenanceStorage Storage { get; } = storage;
        internal SinkProvenanceState State { get; } = state;
        internal bool RestrictedEvidence { get; set; }
    }
}
