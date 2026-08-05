// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

/// <summary>
/// Discovery isolates a projection it cannot build so the rest of the read side survives, and logs it. A log line is
/// something a consumer is told, not something it can ask about - so the failure is kept per artifact as well, which is
/// what lets an unbuildable read model be told apart from one that was never declared.
/// </summary>
public class and_one_projection_definition_cannot_be_built : given.all_dependencies
{
    Projections _projections;

    void Establish()
    {
        _clientArtifacts.Projections.Returns(
        [
            typeof(BuildableProjection),
            typeof(UnbuildableProjection)
        ]);

        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<BuildableModel>>(typeof(BuildableProjection))
            .Returns(new BuildableProjection());
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<UnbuildableModel>>(typeof(UnbuildableProjection))
            .Returns(new UnbuildableProjection());

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

    [Fact] void should_only_define_the_projection_that_could_be_built() => _projections.Definitions.Count.ShouldEqual(1);
    [Fact] void should_report_an_outcome_for_every_declared_projection() => _projections.ArtifactRegistrations.Select(_ => _.ArtifactType).ShouldContainOnly([typeof(BuildableProjection), typeof(UnbuildableProjection)]);
    [Fact] void should_report_the_projection_that_could_be_built_as_registered() => Outcome<BuildableProjection>().IsRegistered.ShouldBeTrue();
    [Fact] void should_not_report_the_projection_that_could_not_be_built_as_registered() => Outcome<UnbuildableProjection>().IsRegistered.ShouldBeFalse();
    [Fact] void should_carry_the_failure_that_stopped_it() => Outcome<UnbuildableProjection>().Failure.ShouldBeOfExactType<ProjectionCannotBeDefined>();

    ArtifactRegistration Outcome<TArtifact>() => _projections.ArtifactRegistrations.First(_ => _.ArtifactType == typeof(TArtifact));

    public record BuildableModel();

    public record UnbuildableModel();

    public class BuildableProjection : IProjectionFor<BuildableModel>
    {
        public void Define(IProjectionBuilderFor<BuildableModel> builder)
        {
        }
    }

    public class UnbuildableProjection : IProjectionFor<UnbuildableModel>
    {
        public void Define(IProjectionBuilderFor<UnbuildableModel> builder) => throw new ProjectionCannotBeDefined();
    }

    /// <summary>
    /// The exception that is thrown when a projection in this specification cannot define itself.
    /// </summary>
    public class ProjectionCannotBeDefined() : Exception("The projection cannot be defined");
}
