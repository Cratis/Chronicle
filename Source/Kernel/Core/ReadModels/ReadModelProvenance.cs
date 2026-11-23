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
/// <param name="storage">The authoritative definition and sink storage.</param>
/// <param name="eventStore">The event store to inventory.</param>
internal sealed class ReadModelProvenance(IStorage storage, EventStoreName eventStore)
{
    readonly IEventStoreStorage _storage = storage.GetEventStore(eventStore);
    readonly Lock _gate = new();
    Task _pending = Task.CompletedTask;
    IReadOnlyList<ReadModelDefinition>? _legacy;

    /// <summary>
    /// Persists all discoverable evidence before a definition can be changed.
    /// </summary>
    /// <returns>The preservation task.</returns>
    internal Task Preserve() => Serialize(PreserveEvidence);

    /// <summary>
    /// Checks the live database marker and retained definition before admitting a read.
    /// </summary>
    /// <param name="namespace">The actual destination namespace.</param>
    /// <param name="definition">The requested definition.</param>
    /// <returns>The admission task.</returns>
    internal Task EnsureReadable(EventStoreNamespaceName @namespace, ReadModelDefinition definition) =>
        Serialize(() => CheckEvidence(@namespace, definition));

    static async Task RunAfter(Task previous, Func<Task> operation)
    {
        // Each caller observes its own failure; a failed inventory must not prevent a later retry.
        await previous.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
        await operation();
    }

    Task Serialize(Func<Task> operation)
    {
        lock (_gate)
        {
            return _pending = RunAfter(_pending, operation);
        }
    }

    async Task PreserveEvidence()
    {
        var candidates = await Candidates();
        var namespaces = (await _storage.Namespaces.GetAll()).Select(_ => _.Name)
            .Append(EventStoreNamespaceName.Default).Append(EventStoreNamespaceName.NotSet).Distinct();
        foreach (var @namespace in namespaces) await Mark(@namespace, candidates);
    }

    async Task CheckEvidence(EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
        await Mark(@namespace, await Candidates());
        await Check(@namespace, definition);
        if (await _storage.ReadModels.Has(definition.Identifier))
        {
            await Check(@namespace, await _storage.ReadModels.Get(definition.Identifier));
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
