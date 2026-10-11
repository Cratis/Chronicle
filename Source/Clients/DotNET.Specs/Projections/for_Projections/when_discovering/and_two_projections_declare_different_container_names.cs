// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_two_projections_declare_different_container_names : given.all_dependencies
{
    Projections _projections;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(FirstProjection), typeof(SecondProjection)]);
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheModel>>(typeof(FirstProjection))
            .Returns(new FirstProjection());
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheModel>>(typeof(SecondProjection))
            .Returns(new SecondProjection());

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_keep_the_first_container_name() => ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(TheModel)).ShouldEqual("first");
    [Fact] void should_only_define_the_first_projection() => _projections.Definitions.Count.ShouldEqual(1);
    [Fact] void should_report_the_conflict() => _projections.ArtifactRegistrations.Single(_ => !_.IsRegistered).Failure.ShouldBeOfExactType<ConflictingContainerNamesForReadModel>();

    public record TheModel(string Id);

    public class FirstProjection : IProjectionFor<TheModel>
    {
        public void Define(IProjectionBuilderFor<TheModel> builder) => builder.ContainerName("first");
    }

    public class SecondProjection : IProjectionFor<TheModel>
    {
        public void Define(IProjectionBuilderFor<TheModel> builder) => builder.ContainerName("second");
    }
}
