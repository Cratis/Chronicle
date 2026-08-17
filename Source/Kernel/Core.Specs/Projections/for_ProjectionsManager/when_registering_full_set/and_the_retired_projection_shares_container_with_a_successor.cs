// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// The renamed-read-model shape from https://github.com/Cratis/Chronicle/issues/3725: the read model kept its type
/// name (thus its container name) but moved namespace, so the old projection is retired while its successor writes
/// the same container. The successor must get a replay recommendation so its container can be rebuilt cleanly -
/// the container itself is never dropped.
/// </summary>
public class and_the_retired_projection_shares_container_with_a_successor : given.a_projections_manager_grain
{
    const string SharedContainer = "boards";
    ProjectionDefinition _successor;
    ProjectionDefinition _orphan;

    void Establish()
    {
        _successor = CreateDefinition("renamed-projection", "renamed-read-model");
        _orphan = CreateDefinition("original-projection", "original-read-model");
        _state.Projections = [_successor, _orphan];
        _readModelDefinitions =
        [
            CreateReadModelDefinition("renamed-read-model", SharedContainer),
            CreateReadModelDefinition("original-read-model", SharedContainer)
        ];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_successor], ProjectionOwner.Client);

    [Fact] void should_retire_the_original_projection() => _state.Projections.ShouldContainOnly(_successor);
    [Fact]
    void should_recommend_replaying_the_successor() =>
        _recommendationsManager.Received(1).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            Arg.Is<Concepts.Recommendations.RecommendationDescription>(description => description.Value.Contains(SharedContainer)),
            Arg.Is<ReplayCandidateRequest>(request =>
                request.ObserverId.Value == "renamed-projection" &&
                request.Reasons.Any(reason => reason.Type == ReplayCandidateReasonType.RetiredProjectionSharedContainer)));
}
