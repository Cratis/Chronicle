// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the legacy, pass-through implementation of <see cref="IReadModelReadBindings"/>.
/// </summary>
/// <param name="storage">The storage from which to resolve the sink.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for looking up the occurrences of a read model.</param>
/// <param name="visibilities">The <see cref="IReadModelInstanceVisibilities"/> deciding which stored roots are read.</param>
public class ReadModelReadBindings(IStorage storage, IGrainFactory grainFactory, IReadModelInstanceVisibilities visibilities) : IReadModelReadBindings
{
    /// <inheritdoc/>
    public async Task<ReadModelReadBinding> Resolve(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        ReadModelContainerName? occurrence = null)
    {
        await EnsureKnownOccurrence(eventStore, eventStoreNamespace, definition, occurrence);
        var sinks = storage.GetEventStore(eventStore).GetNamespace(eventStoreNamespace).Sinks;

        // Reading an occurrence must not provision the primary container or its indexes.
        var isOccurrence = occurrence is not null && occurrence.Value != definition.ContainerName.Value;
        var sink = isOccurrence
            ? await sinks.GetWithoutProvisioning(definition)
            : await sinks.GetFor(definition, definition.ContainerName);
        var visibility = await visibilities.GetFor(eventStore, eventStoreNamespace, definition);
        return new(sink, occurrence, visibility);
    }

    async Task EnsureKnownOccurrence(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        ReadModelContainerName? occurrence)
    {
        if (occurrence is null || occurrence.Value == definition.ContainerName.Value) return;

        var occurrences = await grainFactory.GetReadModelReplayManager(eventStore, eventStoreNamespace, definition.Identifier).GetOccurrences();
        if (!occurrences.Any(_ => string.Equals(_.RevertContainerName.Value, occurrence.Value, StringComparison.Ordinal)))
        {
            throw new UnknownReadModelOccurrence(definition.Identifier);
        }
    }
}
