// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Grpc.Core;
using Orleans;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Admits public reads using authoritative producer metadata and the actual physical sink's marker.
/// </summary>
public static class ReadModelPublicReadGuard
{
    /// <summary>
    /// Checks before snapshot bootstrapping, primary-sink access or a raw-event fallback.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <param name="definition">The requested or draft definition.</param>
    /// <returns>The admission task.</returns>
    public static async Task Ensure(IStorage storage, IGrainFactory grainFactory, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var canonical = await eventStoreStorage.ReadModels.Has(definition.Identifier)
            ? await eventStoreStorage.ReadModels.Get(definition.Identifier)
            : definition;
        var provenance = ReadModelProvenance.For(storage, eventStore);
        await provenance.EnsureReadable(@namespace, definition);
        await ReadModelReadPolicy.EnsureReadable(grainFactory, eventStore, canonical);
        await EnsureReducer(eventStoreStorage, canonical);
        if (canonical.ObserverType == ReadModelObserverType.Projection && await eventStoreStorage.Projections.Has((ProjectionId)canonical.ObserverIdentifier.Value))
        {
            var projection = await eventStoreStorage.Projections.Get((ProjectionId)canonical.ObserverIdentifier.Value);
            if (projection.Scope == ProjectionScope.Global)
            {
                await provenance.EnsureReadable(EventStoreNamespaceName.Default, canonical);
            }
        }
    }

    /// <summary>
    /// Checks a reducer registration before any initial state can be delivered to its connected client.
    /// </summary>
    /// <param name="storage">The required storage; absence must fail closed.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="definition">The registered read model.</param>
    /// <returns>The admission task.</returns>
    public static Task EnsureRegistration(IStorage? storage, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition definition) =>
        ReadModelProvenance.For(storage ?? throw new InvalidOperationException("Read-model storage is not available."), eventStore).EnsureReadable(@namespace, definition);

    static async Task EnsureReducer(IEventStoreStorage storage, ReadModelDefinition definition)
    {
        if (definition.ObserverType != ReadModelObserverType.Reducer) return;
        var id = (ReducerId)definition.ObserverIdentifier.Value;
        if (!await storage.Reducers.Has(id) || (await storage.Reducers.Get(id)).EventSequenceId.IsSystemSequence())
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }
}
