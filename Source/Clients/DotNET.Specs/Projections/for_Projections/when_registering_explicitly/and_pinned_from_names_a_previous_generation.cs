// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;

using Generations = Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given.a_builder_with_generations;

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class and_pinned_from_names_a_previous_generation : given.projections_for_explicit_registration
{
    void Establish() => _projections = new(_eventStore, new EventTypesForSpecifications([typeof(Generations.First), typeof(Generations.Second)]), _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance) { GenerationDelivery = EventGenerationDelivery.Pinned };

    async Task Because() => await _projections.Register<Inventory>(projection => projection.From<Generations.First>());

    [Fact] void should_register_the_previous_generation_clause() => _projections.Definitions.Single().From.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_opt_in_to_pinned_delivery() => _projections.Definitions.Single().GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
}
