// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;
using Microsoft.Extensions.Logging.Abstractions;

using Generations = Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given.a_builder_with_generations;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_model_bound_projection_pins_two_generations_of_one_type : given.all_dependencies
{
    Projections _projections;

    void Establish()
    {
        _clientArtifacts.ModelBoundProjections.Returns([typeof(Model)]);
        _projections = new(_eventStore, new EventTypesForSpecifications([typeof(Generations.First), typeof(Generations.Second)]), _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance) { GenerationDelivery = EventGenerationDelivery.Pinned };
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_report_the_invalid_pins() => _projections.ArtifactRegistrations.Single().Failure.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
    [Fact] void should_not_include_the_invalid_definition() => _projections.Definitions.ShouldBeEmpty();

    [FromEvent<Generations.First>]
    [FromEvent<Generations.Second>]
    public record Model(string Name);
}
