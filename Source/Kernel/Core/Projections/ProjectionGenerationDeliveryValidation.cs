// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Validates generation pins across every level of a projection definition.
/// </summary>
internal static class ProjectionGenerationDeliveryValidation
{
    /// <summary>
    /// Rejects several generations of one event type in a pinned projection.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <exception cref="ObserverPinsMultipleGenerationsOfEventType">The projection pins several generations of one event type.</exception>
    internal static void Validate(ProjectionDefinition definition)
    {
        if (definition.GenerationDelivery != EventGenerationDelivery.Pinned)
        {
            return;
        }

        var duplicate = EventTypesFor(definition).GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(type => type.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new ObserverPinsMultipleGenerationsOfEventType(duplicate.Key);
        }
    }

    static IEnumerable<EventType> EventTypesFor(ProjectionDefinition definition) =>
        definition.From.Keys
            .Concat(definition.Join.Keys)
            .Concat(definition.RemovedWith.Keys)
            .Concat(definition.RemovedWithJoin.Keys)
            .Concat(definition.FromDerivatives.SelectMany(_ => _.EventTypes))
            .Concat(definition.FromEventProperty is { } property ? [property.Event] : [])
            .Concat(definition.Children.Values.SelectMany(EventTypesFor))
            .Concat(definition.Nested?.Values.SelectMany(EventTypesFor) ?? []);
}
