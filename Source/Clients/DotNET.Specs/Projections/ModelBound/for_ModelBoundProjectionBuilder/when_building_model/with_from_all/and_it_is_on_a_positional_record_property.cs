// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_from_all;

public class and_it_is_on_a_positional_record_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ThingHappened)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(ThingTimeline));

    [Fact]
    void should_pick_up_the_property_targeted_from_all()
    {
        // [property: FromAll] on a positional record parameter applies to the generated property — the
        // only way to place the property-only [FromAll] there — and must still be picked up.
        _result.All.Properties.Count.ShouldEqual(1);
    }

    [Fact]
    void should_map_it_as_a_context_property()
    {
        // Mapped as a CONTEXT property ($eventContext(...)), not mis-converted into an event-property
        // lookup — the FromAll to FromEvery argument order must not be swapped.
        _result.All.Properties.Values.Single().ShouldEqual($"{WellKnownExpressions.EventContext}(Occurred)");
    }
}

[EventType]
public record ThingHappened(string Name);

public record ThingTimeline(
    Guid Id,
    string Name,
    [property: FromAll(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset? LastUpdatedAt);
