// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

public class and_global_kernel_statistics_are_no_longer_in_the_set : given.a_projections_manager_grain
{
    readonly JobId _runningReplay = JobId.New();
    ProjectionDefinition _remaining;
    ProjectionDefinition _legacy;

    void Establish()
    {
        _remaining = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.Single();
        _legacy = _remaining with { Identifier = $"{_remaining.Identifier.Value}.global", Scope = ProjectionScope.Global };
        _state.Projections = [_remaining, _legacy];
        _namespaces.GetAll().Returns(["first", "second"]);
        var request = new ReplayObserverPartitionRequest(
            new ObserverKey(_legacy.Identifier.Value, EventStore, "first", EventSequenceId.Log),
            ObserverType.Projection,
            "partition",
            EventSequenceNumber.First,
            EventSequenceNumber.Max,
            []);
        _jobsManager.GetAllJobs().Returns(ImmutableList.Create(new JobState
        {
            Id = _runningReplay,
            Status = JobStatus.Running,
            Request = request
        }));
        _readModelDefinitions = [CreateReadModelDefinition(_remaining.ReadModel, nameof(EventTypeStatistics))];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_remaining], ProjectionOwner.Kernel);

    [Fact] void should_keep_only_the_namespaced_projection() => _state.Projections.ShouldContainOnly(_remaining);
    [Fact] async Task should_retire_the_legacy_observer_in_every_namespace() => await _observerGrain.Received(2).Retire();
    [Fact] async Task should_inspect_jobs_in_every_namespace() => await _jobsManager.Received(2).GetAllJobs();
    [Fact] async Task should_delete_a_persisted_running_replay() => await _jobsManager.Received(2).Delete(_runningReplay);
    [Fact] async Task should_unregister_the_shared_writer() => await _projectionsServiceClient.Received(1).Unregister((EventStoreName)EventStore, _legacy.Identifier);
    [Fact] async Task should_remove_the_legacy_definition() => await _projectionGrain.Received(1).Remove();
    [Fact] async Task should_preserve_the_namespaced_read_model_definition() => await _eventStoreStorage.ReadModels.DidNotReceive().Delete(_remaining.ReadModel);
}
