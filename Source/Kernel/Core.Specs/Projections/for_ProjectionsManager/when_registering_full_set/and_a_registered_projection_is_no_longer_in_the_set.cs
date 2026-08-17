// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// The retirement shape behind https://github.com/Cratis/Chronicle/issues/3725: the client re-registers its full
/// set of projections and one previously registered projection is no longer in it (its read model was deleted).
/// The orphan is retired - observer unsubscribed, engine and storage cleaned - and since no other projection
/// targets its container, no replay recommendation is raised.
/// </summary>
public class and_a_registered_projection_is_no_longer_in_the_set : given.a_projections_manager_grain
{
    ProjectionDefinition _remaining;
    ProjectionDefinition _orphan;

    void Establish()
    {
        _remaining = CreateDefinition("remaining-projection", "remaining-read-model");
        _orphan = CreateDefinition("orphaned-projection", "orphaned-read-model");
        _state.Projections = [_remaining, _orphan];
        _readModelDefinitions =
        [
            CreateReadModelDefinition("remaining-read-model", "remainingContainer"),
            CreateReadModelDefinition("orphaned-read-model", "orphanedContainer")
        ];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_remaining], ProjectionOwner.Client);

    [Fact] void should_unsubscribe_the_orphaned_observer() => _observerGrain.Received(1).Unsubscribe();
    [Fact] void should_unregister_the_orphan_from_the_engine() => _projectionsServiceClient.Received(1).Unregister((EventStoreName)EventStore, (ProjectionId)"orphaned-projection");
    [Fact] void should_remove_the_orphaned_projection_grain() => _projectionGrain.Received(1).Remove();
    [Fact] void should_clear_the_orphaned_failed_partitions() => _failedPartitionsStorage.Received(1).Save((ObserverId)_orphan.Identifier.Value, Arg.Is<FailedPartitions>(failedPartitions => !failedPartitions.HasFailedPartitions));
    [Fact] void should_keep_only_the_remaining_projection_in_the_registered_state() => _state.Projections.ShouldContainOnly(_remaining);
    [Fact] void should_not_recommend_replay_when_no_projection_shares_the_container() => _recommendationsManager.DidNotReceiveWithAnyArgs().Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
}
