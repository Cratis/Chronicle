// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Recommendations;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_pruning_orphaned_recommendations_fails : given.a_startup_task_with_an_empty_namespace
{
    readonly RecommendationId _healthy = RecommendationId.New();
    IRecommendationStorage _failedRecommendations;
    IRecommendationStorage _healthyRecommendations;

    void Establish()
    {
        _failedRecommendations = _eventStoreStorage.GetNamespace(_emptyNamespace).Recommendations;
        _failedRecommendations.GetAll().Returns(Task.FromException<IImmutableList<RecommendationState>>(new IOException("Simulated storage failure")));
        _healthyRecommendations = _namespaceStorage.Recommendations;
        _healthyRecommendations.GetAll().Returns(ImmutableList.Create(new RecommendationState
        {
            Id = _healthy,
            Occurred = DateTimeOffset.UtcNow.AddDays(-1),
            Request = new ReplayCandidateRequest { ObserverId = "missing" }
        }));
        _observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(_ => new ObserverState());
    }

    Task Because() => Execute();

    [Fact] async Task should_not_retry_the_failed_sweep() => await _failedRecommendations.Received(1).GetAll();
    [Fact] async Task should_still_prune_the_healthy_namespace() => await _healthyRecommendations.Received(1).Remove(_healthy);
    [Fact] void should_log_a_warning() => LogEntries.ShouldContain(_ => _.Level == LogLevel.Warning && _.Event == "PruningOrphanedRecommendationsFailed");
    [Fact] void should_finish_startup() => _bootstrapClientsEnsured.ShouldBeTrue();
}
