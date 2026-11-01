// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;

using Generations = Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given.a_builder_with_generations;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_variant_with_a_container_name_is_removed_by_revalidation : given.all_dependencies
{
    Projections _projections;
    IProjectionHandler _handler;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(VariantA), typeof(VariantB)]);
        _clientArtifacts.ModelBoundProjections.Returns([]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<ModelA>>(typeof(VariantA)).Returns(new VariantA());
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<ModelB>>(typeof(VariantB)).Returns(new VariantB());
        _projections = new(
            _eventStore,
            new EventTypesForSpecifications([typeof(Generations.First), typeof(Generations.Second)]),
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance)
        { GenerationDelivery = EventGenerationDelivery.Pinned };
    }

    async Task Because()
    {
        await _projections.Discover();
        _handler = await _projections.Register<ModelA>(projection => projection.From<Generations.First>());
    }

    [Fact] void should_remove_both_variant_definitions_but_keep_the_explicit_one() => _projections.Definitions.Count.ShouldEqual(1);
    [Fact] void should_not_keep_the_container_name_of_the_removed_variant() => ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(ModelA)).ShouldBeNull();
    [Fact] void should_report_the_container_the_kernel_is_told() => _handler.ContainerName.Value.ShouldEqual(((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(ModelA)) ?? _namingPolicy.GetReadModelName(typeof(ModelA)));
    [Fact] void should_report_the_naming_policy_container() => _handler.ContainerName.Value.ShouldEqual(_namingPolicy.GetReadModelName(typeof(ModelA)));

    public record ModelA(string Name);

    public record ModelB(string Name);

    public class VariantA : IProjectionFor<ModelA>
    {
        public void Define(IProjectionBuilderFor<ModelA> builder) => builder
            .ContainerName("variant-a")
            .VariantOf<Identity>(_ => _.Name)
            .EntersOn<Generations.First>()
            .From<Generations.First>();
    }

    public class VariantB : IProjectionFor<ModelB>
    {
        public void Define(IProjectionBuilderFor<ModelB> builder) => builder
            .VariantOf<Identity>(_ => _.Name)
            .EntersOn<Generations.Second>()
            .From<Generations.Second>();
    }

    public class Identity;
}
