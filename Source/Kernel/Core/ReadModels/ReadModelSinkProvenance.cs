// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Coordinates storage-wide inventory before admitting any physical read-model database.
/// </summary>
/// <param name="storage">The authoritative definition and sink storage.</param>
[Singleton]
public class ReadModelSinkProvenance(IStorage storage) : IReadModelSinkProvenance
{
    readonly SemaphoreSlim _inventory = new(1, 1);

    /// <inheritdoc/>
    public Task Preserve(EventStoreName eventStore) => Inventory(eventStore);

    /// <inheritdoc/>
    public async Task EnsureReadable(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition definition)
    {
        if (SinkProvenanceExemptions.IsNonDatabase(definition.Sink.TypeId))
        {
            if (!SinkProvenanceExemptions.AllowsPublicReads(definition.Sink.TypeId)) throw new ReadModelIsNotAvailable();
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
            if (!state.IsComplete || state.Restricted || await provenance.IsQuarantined()) throw new ReadModelIsNotAvailable();
        }
        catch (Exception exception) when (exception is not ReadModelIsNotAvailable)
        {
            throw new ReadModelIsNotAvailable();
        }
    }

    async Task Inventory(EventStoreName eventStore, EventStoreNamespaceName? @namespace = null, ReadModelDefinition? definition = null)
    {
        await _inventory.WaitAsync();
        try
        {
            await new ReadModelProvenance(storage).Inventory(eventStore, @namespace, definition);
        }
        catch (Exception exception) when (exception is not ReadModelIsNotAvailable)
        {
            throw new ReadModelIsNotAvailable();
        }
        finally
        {
            _inventory.Release();
        }
    }
}
