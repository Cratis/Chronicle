// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class independent_key_parameter_with_event_source_key : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(IndependentKeyOwnerCreated), typeof(IndependentKeyChildAdded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(IndependentKeyOwner));

    [Fact]
    void should_map_the_key_parameter_to_the_event_source_id()
    {
        var eventType = event_types.GetEventTypeFor(typeof(IndependentKeyChildAdded)).ToContract();
        var from = _result.Children[nameof(IndependentKeyOwner.Children)].From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        from.Properties[nameof(IndependentKeyChild.SourceId)].ShouldEqual("$eventContext(EventSourceId)");
    }
}

[EventType]
public record IndependentKeyOwnerCreated(Guid Id);

[EventType]
public record IndependentKeyChildAdded(Guid Id);

public record IndependentKeyChild([Key] Guid SourceId, Guid Id);

[FromEvent<IndependentKeyOwnerCreated>]
public record IndependentKeyOwner(
    Guid Id,
    [ChildrenFrom<IndependentKeyChildAdded>(identifiedBy: nameof(IndependentKeyChild.Id))]
    IEnumerable<IndependentKeyChild> Children);

#pragma warning restore SA1402 // File may only contain a single type
