// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Preserves evidence and checks live provenance for one event store.
/// </summary>
internal sealed class ReadModelProvenance(IStorage storage, EventStoreName eventStore)
{
    readonly IEventStoreStorage _storage = storage.GetEventStore(eventStore);
    readonly SemaphoreSlim _gate = new(1, 1);
    IReadOnlyList<ReadModelDefinition>? _legacy;

    internal async Task Preserve()
    {
        await _gate.WaitAsync();
        try
        {
            var candidates = await Candidates();
            var namespaces = (await _storage.Namespaces.GetAll()).Select(_ => _.Name)
                .Append(EventStoreNamespaceName.Default).Append(EventStoreNamespaceName.NotSet).Distinct();
            foreach (var @namespace in namespaces) await Mark(@namespace, candidates);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task EnsureReadable(EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
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
        if (SinkProvenanceExemptions.IsNonDatabase(definition.Sink.Type)) return;
        if (await _storage.GetNamespace(@namespace).Sinks.GetProvenance(definition).IsQuarantined())
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }

    async Task<IEnumerable<ReadModelDefinition>> Candidates()
    {
        // Capture unresolved legacy bindings before the first registration. An ordinary new registration
        // can persist its read model before its producer, without adding evidence of protected data.
        _legacy ??= await LegacyReadModelSinkInventory.Find(_storage, includeUnresolved: true);
        return _legacy.Concat(await LegacyReadModelSinkInventory.Find(_storage, includeUnresolved: false));
    }

    async Task Mark(EventStoreNamespaceName @namespace, IEnumerable<ReadModelDefinition> candidates)
    {
        var sinks = _storage.GetNamespace(@namespace).Sinks;
        foreach (var definition in candidates.Where(_ => !SinkProvenanceExemptions.IsNonDatabase(_.Sink.Type)))
        {
            await sinks.GetProvenance(definition).MarkQuarantined();
        }
    }
}
