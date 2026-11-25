// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Coordinates storage-wide inventory before admitting any physical read-model database.
/// </summary>
/// <param name="storage">The authoritative definition and sink storage.</param>
[Singleton]
public class ReadModelSinkProvenance(IStorage storage) : IReadModelSinkProvenance
{
    readonly Lock _gate = new();
    Task _pending = Task.CompletedTask;

    /// <inheritdoc/>
    /// <exception cref="RpcException">Inventory could not establish database eligibility.</exception>
    public Task Preserve(EventStoreName eventStore) => Inventory(eventStore);

    /// <inheritdoc/>
    /// <exception cref="RpcException">The database cannot authorize the requested read.</exception>
    public async Task EnsureReadable(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
        if (SinkProvenanceExemptions.IsNonDatabase(definition.Sink.Type))
        {
            if (!SinkProvenanceExemptions.AllowsPublicReads(definition.Sink.Type)) throw Unavailable();
            return;
        }

        try
        {
            var provenance = storage.GetEventStore(eventStore).GetNamespace(@namespace).Sinks.GetProvenance(definition);
            if (!(await provenance.ReadState()).Validate().IsComplete)
            {
                await Inventory(eventStore, @namespace, definition);
            }

            var state = (await provenance.ReadState()).Validate();
            if (!state.IsComplete || state.Restricted || await provenance.IsQuarantined()) throw Unavailable();
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw Unavailable();
        }
    }

    static RpcException Unavailable() => new(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));

    static async Task RunAfter(Task previous, Func<Task> operation)
    {
        // Observe each failure at its caller, without poisoning later cold admission attempts.
        await previous.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
        await operation();
    }

    Task Inventory(EventStoreName eventStore, EventStoreNamespaceName? @namespace = null, ReadModelDefinition? definition = null)
    {
        lock (_gate)
        {
            return _pending = RunAfter(_pending, () => InventoryCore(eventStore, @namespace, definition));
        }
    }

    async Task InventoryCore(EventStoreName eventStore, EventStoreNamespaceName? @namespace, ReadModelDefinition? definition)
    {
        try
        {
            if (definition is not null && @namespace is not null &&
                (await storage.GetEventStore(eventStore).GetNamespace(@namespace).Sinks.GetProvenance(definition).ReadState()).Validate().IsComplete)
            {
                return;
            }

            await new ReadModelProvenance(storage).Inventory(eventStore, @namespace, definition);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw Unavailable();
        }
    }
}
