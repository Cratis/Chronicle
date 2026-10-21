// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;
using Microsoft.Extensions.Logging.Abstractions;

using Generations = Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given.a_builder_with_generations;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class with_pinned_delivery_and_previous_generation_clauses : given.all_dependencies
{
    Projections _projections;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(Projection)]);
        _clientArtifacts.ModelBoundProjections.Returns([typeof(ModelBoundModel)]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<Generations.Model>>(typeof(Projection)).Returns(new Projection());
        _projections = new(_eventStore, new EventTypesForSpecifications([typeof(Generations.First), typeof(Generations.Second)]), _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance) { GenerationDelivery = EventGenerationDelivery.Pinned };
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_pin_the_fluent_previous_generation() => _projections.Definitions.Single(definition => definition.ReadModel == typeof(Generations.Model).FullName).From.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_pin_the_model_bound_previous_generation() => _projections.Definitions.Single(definition => definition.ReadModel == typeof(ModelBoundModel).FullName).From.Keys.Single().Generation.ShouldEqual(1U);
    [Fact] void should_opt_both_definitions_into_pinned_delivery() => _projections.Definitions.All(definition => definition.GenerationDelivery == Contracts.Observation.EventGenerationDelivery.Pinned).ShouldBeTrue();

    public class Projection : IProjectionFor<Generations.Model>
    {
        public void Define(IProjectionBuilderFor<Generations.Model> builder) => builder.From<Generations.First>();
    }

    [FromEvent<Generations.First>]
    public record ModelBoundModel(string Name);
}
