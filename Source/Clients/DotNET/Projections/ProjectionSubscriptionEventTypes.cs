// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Collects the event types used by a projection and its descendants for external subscriptions.
/// </summary>
internal static class ProjectionSubscriptionEventTypes
{
    /// <summary>
    /// Gets the event type identifiers used by a projection, including removal clauses.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <returns>The event type identifiers used by the definition tree.</returns>
    internal static IEnumerable<EventTypeId> For(ProjectionDefinition definition) =>
        definition.From.Keys
            .Concat(definition.Join.Keys)
            .Concat(definition.RemovedWith.Keys)
            .Concat(definition.RemovedWithJoin.Keys)
            .Select(eventType => new EventTypeId(eventType.Id))
            .Concat(definition.Children.Values.SelectMany(For))
            .Concat(definition.Nested.Values.SelectMany(For));

    static IEnumerable<EventTypeId> For(ChildrenDefinition definition) =>
        definition.From.Keys
            .Concat(definition.Join.Keys)
            .Concat(definition.RemovedWith.Keys)
            .Concat(definition.RemovedWithJoin.Keys)
            .Select(eventType => new EventTypeId(eventType.Id))
            .Concat(definition.Children.Values.SelectMany(For))
            .Concat(definition.Nested.Values.SelectMany(For));
}
