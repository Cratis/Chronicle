// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Preflights a complete definition change before any member of the batch is persisted.
/// </summary>
internal static class ReadModelDefinitionProvenance
{
    internal static async Task Ensure(
        IStorage storage,
        IReadModelSinkProvenance provenance,
        EventStoreName eventStore,
        IEnumerable<ReadModelDefinition> definitions)
    {
        await provenance.Preserve(eventStore);
        var namespaces = (await storage.GetEventStore(eventStore).Namespaces.GetAll()).Select(_ => _.Name)
            .Append(EventStoreNamespaceName.Default).Append(EventStoreNamespaceName.NotSet).Distinct().ToArray();
        foreach (var definition in definitions)
        {
            foreach (var @namespace in namespaces)
            {
                await provenance.EnsureReadable(eventStore, @namespace, definition);
            }
        }
    }
}
