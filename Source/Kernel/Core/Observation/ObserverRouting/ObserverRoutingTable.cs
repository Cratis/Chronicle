// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents the immutable, silo-local routing table of one event store: which observers take which events.
/// </summary>
/// <remarks>
/// It is the read side only. It is built from the definitions held for the event store, never written to, and replaced as
/// a whole when one of them changes, so a reader holds one consistent view for as long as it needs it. The routes of every
/// observer are indexed by event sequence and event type, so matching an event costs one lookup whatever the number of
/// observers.
/// </remarks>
public sealed class ObserverRoutingTable
{
    readonly FrozenDictionary<ObserverId, ObserverRoute> _routes;
    readonly FrozenDictionary<(EventSequenceId, EventTypeId), ObserverRoute[]> _byEventType;
    readonly FrozenDictionary<EventSequenceId, ObserverRoute[]> _allEventTypes;

    ObserverRoutingTable(long revision, IEnumerable<ObserverRoute> routes)
    {
        Revision = revision;
        _routes = routes.ToFrozenDictionary(_ => _.ObserverId);
        _byEventType = _routes.Values
            .Where(_ => !_.AllEventTypes)
            .SelectMany(route => route.EventTypes.Ids.Select(eventTypeId => (Key: (route.EventSequenceId, eventTypeId), Route: route)))
            .GroupBy(_ => _.Key, _ => _.Route)
            .ToFrozenDictionary(_ => _.Key, _ => _.ToArray());
        _allEventTypes = _routes.Values
            .Where(_ => _.AllEventTypes)
            .GroupBy(_ => _.EventSequenceId)
            .ToFrozenDictionary(_ => _.Key, _ => _.ToArray());
    }

    /// <summary>
    /// Gets the empty table, which routes nothing.
    /// </summary>
    public static ObserverRoutingTable Empty { get; } = new(0, []);

    /// <summary>
    /// Gets the revision of the snapshot of definitions the table was built from.
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// Gets every route in the table.
    /// </summary>
    public IEnumerable<ObserverRoute> Routes => _routes.Values;

    /// <summary>
    /// Builds a table from a snapshot of the definitions of an event store.
    /// </summary>
    /// <param name="snapshot">The <see cref="ObserverDefinitionRegistrySnapshot"/> to build it from.</param>
    /// <returns>The <see cref="ObserverRoutingTable"/>.</returns>
    public static ObserverRoutingTable From(ObserverDefinitionRegistrySnapshot snapshot) =>
        new(snapshot.Revision, snapshot.Definitions.Values.Select(ObserverRoute.From));

    /// <summary>
    /// Try to get the route of an observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer.</param>
    /// <param name="route">The <see cref="ObserverRoute"/> when found.</param>
    /// <returns>True when found; otherwise false.</returns>
    public bool TryGetRoute(ObserverId observerId, out ObserverRoute route) => _routes.TryGetValue(observerId, out route!);

    /// <summary>
    /// Gets the routes of the observers that take events of a type from an event sequence.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the event was appended to.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> of the event.</param>
    /// <returns>The matching <see cref="ObserverRoute"/>s, observers of every event type included.</returns>
    public IEnumerable<ObserverRoute> Match(EventSequenceId eventSequenceId, EventTypeId eventTypeId)
    {
        var typed = _byEventType.TryGetValue((eventSequenceId, eventTypeId), out var routes) ? routes : [];
        var all = _allEventTypes.TryGetValue(eventSequenceId, out var allRoutes) ? allRoutes : [];
        return all.Length == 0 ? typed : typed.Concat(all);
    }
}
