// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Recommendations;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_orphaned_recommendations_exist : given.a_startup_task_with_an_empty_namespace
{
    readonly RecommendationId _emptyNamespaceOrphan = RecommendationId.New();
    readonly RecommendationId _orphan = RecommendationId.New();
    readonly RecommendationId _known = RecommendationId.New();
    IRecommendationStorage _emptyRecommendations;
    IRecommendationStorage _recommendations;

    void Establish()
    {
        _emptyRecommendations = _eventStoreStorage.GetNamespace(_emptyNamespace).Recommendations;
        _recommendations = _namespaceStorage.Recommendations;
        _emptyRecommendations.GetAll().Returns(ImmutableList.Create(Row(_emptyNamespaceOrphan, "missing-in-empty")));
        _recommendations.GetAll().Returns(ImmutableList.Create(Row(_orphan, "missing"), Row(_known, "known")));
        _observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(_ => new ObserverState());
        _eventStoreStorage.GetNamespace(_emptyNamespace).Observers.Get(Arg.Any<ObserverId>()).Returns(_ => new ObserverState());
        _eventStoreStorage.Observers.Has((ObserverId)"known").Returns(true);
    }

    Task Because() => Execute();

    [Fact] async Task should_prune_the_namespace_with_no_data() => await _emptyRecommendations.Received(1).Remove(_emptyNamespaceOrphan);
    [Fact] async Task should_prune_the_other_namespace() => await _recommendations.Received(1).Remove(_orphan);
    [Fact] async Task should_keep_the_known_subject() => await _recommendations.DidNotReceive().Remove(_known);
    [Fact] void should_log_the_removals() => LogMessages.Count(_ => _.Contains("Pruned 1 orphaned recommendation")).ShouldEqual(2);
    [Fact] void should_finish_startup() => _bootstrapClientsEnsured.ShouldBeTrue();

    static RecommendationState Row(RecommendationId id, string subject) => new()
    {
        Id = id,
        Occurred = DateTimeOffset.UtcNow.AddDays(-1),
        Request = new ReplayCandidateRequest { ObserverId = subject }
    };
}
