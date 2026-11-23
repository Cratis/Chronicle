// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Preserves legacy producer evidence before mutation and checks live database provenance at admission.
/// </summary>
public sealed class ReadModelProvenance
{
    static readonly ConditionalWeakTable<IStorage, ConcurrentDictionary<EventStoreName, ReadModelProvenance>> _instances = new();
    readonly IEventStoreStorage _storage;
    readonly EventStoreName _eventStore;
    readonly SemaphoreSlim _gate = new(1, 1);
    IReadOnlyList<ReadModelDefinition>? _legacy;

    ReadModelProvenance(IStorage storage, EventStoreName eventStore)
    {
        _storage = storage.GetEventStore(eventStore);
        _eventStore = eventStore;
    }

    /// <summary>
    /// Gets the coordinator scoped to a storage instance and event store, never a process-wide destination identity.
    /// </summary>
    /// <param name="storage">The storage instance.</param>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The coordinator.</returns>
    public static ReadModelProvenance For(IStorage storage, EventStoreName eventStore) =>
        _instances.GetOrCreateValue(storage).GetOrAdd(eventStore, name => new(storage, name));

    /// <summary>
    /// Preserves old destinations before evidence is changed, including the default namespace.
    /// </summary>
    /// <returns>The persistence task.</returns>
    public async Task Preserve()
    {
        if (SystemStoreAccess.IsSystem(_eventStore)) return;
        await _gate.WaitAsync();
        try
        {
            var candidates = await Candidates();
            var namespaces = (await _storage.Namespaces.GetAll()).Select(_ => _.Name).Append(EventStoreNamespaceName.Default).Distinct();
            foreach (var @namespace in namespaces) await Mark(@namespace, candidates);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Checks a database on every admission, including after a sink or pipeline cache hit.
    /// </summary>
    /// <param name="namespace">The actual sink namespace.</param>
    /// <param name="definition">The destination definition.</param>
    /// <returns>The admission task.</returns>
    public async Task EnsureReadable(EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
        if (SystemStoreAccess.IsSystem(_eventStore)) return;
        await _gate.WaitAsync();
        try
        {
            await Mark(@namespace, await Candidates());
            await Check(@namespace, definition);
            if (await _storage.ReadModels.Has(definition.Identifier))
            {
                await Check(@namespace, await _storage.ReadModels.Get(definition.Identifier));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task Check(EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
        if (definition.Sink.Type != SinkTypeId.None && await _storage.GetNamespace(@namespace).Sinks.GetProvenance(definition).IsQuarantined())
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }

    async Task<IEnumerable<ReadModelDefinition>> Candidates()
    {
        // Capture unresolved legacy bindings before the first registration. A new, normal registration may
        // temporarily persist its read model before its producer; that is not new evidence of protected data.
        _legacy ??= await ReadModelProvenanceInventory.Find(_storage, includeUnresolved: true);
        return _legacy.Concat(await ReadModelProvenanceInventory.Find(_storage, includeUnresolved: false));
    }

    async Task Mark(EventStoreNamespaceName @namespace, IEnumerable<ReadModelDefinition> candidates)
    {
        var sinks = _storage.GetNamespace(@namespace).Sinks;
        foreach (var definition in candidates.Where(_ => _.Sink.Type != SinkTypeId.None))
        {
            await sinks.GetProvenance(definition).MarkQuarantined();
        }
    }
}
