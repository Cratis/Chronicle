// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Selects the event types whose history contributes to constraint indexes.
/// </summary>
internal static class ConstraintEventTypes
{
    /// <summary>
    /// Gets filters covering every generation of each governed event type.
    /// </summary>
    /// <param name="definitions">The definitions being replayed.</param>
    /// <returns>Distinct type filters; storage matches their ids across generations.</returns>
    internal static EventType[] For(IEnumerable<IConstraintDefinition> definitions) => definitions.SelectMany(IdsFor)
        .Distinct().Select(id => new EventType(id, EventTypeGeneration.First)).ToArray();

    static IEnumerable<EventTypeId> IdsFor(IConstraintDefinition definition) => definition switch
    {
        UniqueConstraintDefinition unique => unique.EventDefinitions.Select(item => item.EventTypeId).Concat(unique.RemovedWith),
        ClosesStreamConstraintDefinition closing => closing.EventTypeIds.Concat(closing.ReopenedBy).Append(GlobalEventTypes.Redaction),
        _ => []
    };
}
