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
/// <param name="visibilities">The <see cref="IReadModelInstanceVisibilities"/> deciding which stored roots are read.</param>
public class ReadModelReadBindings(IStorage storage, IReadModelInstanceVisibilities visibilities) : IReadModelReadBindings
{
    /// <inheritdoc/>
    public async Task<ReadModelReadBinding> Resolve(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        ReadModelContainerName? occurrence = null)
    {
        var sinks = storage.GetEventStore(eventStore).GetNamespace(eventStoreNamespace).Sinks;
        var sink = await sinks.GetFor(definition, definition.ContainerName);
        var visibility = await visibilities.GetFor(eventStore, eventStoreNamespace, definition);
        return new(sink, occurrence, visibility);
    }
}
