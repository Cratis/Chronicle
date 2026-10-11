// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_different_event_types_pin_different_generations : given.a_builder_with_generations
{
    ProjectionDefinition _definition;

    void Establish()
    {
        _builder.From<Other>();
        _builder.Join<Second>(join => join.On(model => model.Name));
    }

    void Because() => _definition = _builder.BuildWithGenerationDelivery(EventGenerationDelivery.Pinned);

    [Fact] void should_pin_the_other_event_type() => _definition.From.Keys.Single().Id.ShouldEqual("Other");
    [Fact] void should_keep_the_other_events_generation() => _definition.From.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_person_event_type() => _definition.Join.Keys.Single().Id.ShouldEqual("PinnedPerson");
    [Fact] void should_keep_the_person_events_generation() => _definition.Join.Keys.Single().Generation.ShouldEqual(2U);
    [Fact] void should_opt_in_to_pinned_delivery() => _definition.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
}
