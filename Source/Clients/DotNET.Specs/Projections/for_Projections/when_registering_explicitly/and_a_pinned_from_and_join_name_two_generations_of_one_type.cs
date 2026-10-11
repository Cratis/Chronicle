// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;

using Generations = Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given.a_builder_with_generations;

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class and_a_pinned_from_and_join_name_two_generations_of_one_type : given.projections_for_explicit_registration
{
    Exception _error;

    void Establish() => _projections = new(_eventStore, new EventTypesForSpecifications([typeof(Generations.First), typeof(Generations.Second)]), _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance) { GenerationDelivery = EventGenerationDelivery.Pinned };

    async Task Because() => _error = await Catch.Exception(() => _projections.Register<Inventory>(projection =>
    {
        projection.From<Generations.First>();
        projection.Join<Generations.Second>(join => join.On(model => model.Name));
    }));

    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
    [Fact] void should_not_include_the_invalid_definition() => _projections.Definitions.ShouldBeEmpty();
    [Fact] void should_not_send_the_definition_to_the_kernel() => _registrations.ShouldBeEmpty();
}
