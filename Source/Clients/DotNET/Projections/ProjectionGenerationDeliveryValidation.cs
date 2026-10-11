// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Applies the client's delivery policy and validates projection generation pins.
/// </summary>
internal static class ProjectionGenerationDeliveryValidation
{
    /// <summary>
    /// Rejects ambiguous pins in a materialized, typed projection.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <param name="generationDelivery">The client's generation delivery policy.</param>
    /// <exception cref="MultipleGenerationsOfEventTypeInObserver">The projection pins several generations of one event type.</exception>
    internal static void Apply(ProjectionDefinition definition, EventGenerationDelivery generationDelivery)
    {
        // Immediate/passive and wildcard projections keep Compatibility until their own delivery lanes.
        definition.GenerationDelivery = definition.IsActive && !definition.SubscribesToAllEvents
            ? (Contracts.Observation.EventGenerationDelivery)generationDelivery
            : Contracts.Observation.EventGenerationDelivery.Compatibility;
        if (definition.GenerationDelivery == Contracts.Observation.EventGenerationDelivery.Pinned)
        {
            GenerationDeliveryValidation.Validate(EventTypesFor(definition));
        }
    }

    static IEnumerable<EventType> EventTypesFor(ProjectionDefinition definition) =>
        definition.From.Keys.Select(_ => _.ToClient())
            .Concat(definition.Join.Keys.Select(_ => _.ToClient()))
            .Concat(definition.RemovedWith.Keys.Select(_ => _.ToClient()))
            .Concat(definition.RemovedWithJoin.Keys.Select(_ => _.ToClient()))
            .Concat(definition.FromEvery.SelectMany(_ => _.EventTypes.Select(type => type.ToClient())))
            .Concat(definition.FromEventProperty is { } property ? [property.Event.ToClient()] : [])
            .Concat(definition.Children.Values.SelectMany(EventTypesFor))
            .Concat(definition.Nested.Values.SelectMany(EventTypesFor));

    static IEnumerable<EventType> EventTypesFor(ChildrenDefinition definition) =>
        definition.From.Keys.Select(_ => _.ToClient())
            .Concat(definition.Join.Keys.Select(_ => _.ToClient()))
            .Concat(definition.RemovedWith.Keys.Select(_ => _.ToClient()))
            .Concat(definition.RemovedWithJoin.Keys.Select(_ => _.ToClient()))
            .Concat(definition.FromEventProperty is { } property ? [property.Event.ToClient()] : [])
            .Concat(definition.Children.Values.SelectMany(EventTypesFor))
            .Concat(definition.Nested.Values.SelectMany(EventTypesFor));
}
