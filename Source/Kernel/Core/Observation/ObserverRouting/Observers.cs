// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <remarks>
/// The table of an event store is rebuilt only when the snapshot of definitions it was built from is replaced, and a
/// rebuilt table is published with one write, so readers never see a table that is half built.
/// </remarks>
/// <param name="stateProvider">The <see cref="IObserverDefinitionsStateProvider"/> the definitions are read through.</param>
[Singleton]
public class Observers(IObserverDefinitionsStateProvider stateProvider) : IObservers
{
    readonly ConcurrentDictionary<EventStoreName, ObserverRoutingTable> _tables = new();

    /// <inheritdoc/>
    public async ValueTask<ObserverRoutingTable> GetTable(EventStoreName eventStore)
    {
        var snapshot = await stateProvider.GetSnapshot(eventStore);
        if (_tables.TryGetValue(eventStore, out var table) && table.Revision == snapshot.Revision && snapshot.Revision != 0)
        {
            return table;
        }

        table = ObserverRoutingTable.From(snapshot);
        _tables[eventStore] = table;
        return table;
    }

    /// <inheritdoc/>
    public async Task<ObserverDefinitionRevision> Acknowledge(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision)
    {
        var held = HeldRevision(await GetTable(eventStore), observerId);
        if (!IsAcknowledged(held, revision))
        {
            stateProvider.Changed(eventStore, observerId);
            held = HeldRevision(await GetTable(eventStore), observerId);
        }

        return held ?? ObserverDefinitionRevision.None;
    }

    static bool IsAcknowledged(ObserverDefinitionRevision? held, ObserverDefinitionRevision revision)
    {
        // A revision of none stands for a deleted definition, which the silo has applied once it holds no route for it.
        return revision == ObserverDefinitionRevision.None ? held is null : held is not null && held.Value >= revision.Value;
    }

    static ObserverDefinitionRevision? HeldRevision(ObserverRoutingTable table, ObserverId observerId) =>
        table.TryGetRoute(observerId, out var route) ? route.Revision : null;
}
