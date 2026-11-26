// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.given;

public class a_pruner : Specification
{
    protected DateTimeOffset _now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    protected IEventStoreStorage _store;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IObserverStateStorage _observers;
    protected IRecommendationStorage _recommendations;
    protected readonly List<RecommendationState> _rows = [];
    protected readonly List<RecommendationId> _removed = [];
    protected int _count;

    void Establish()
    {
        _store = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _observers = Substitute.For<IObserverStateStorage>();
        _recommendations = Substitute.For<IRecommendationStorage>();
        _namespaceStorage.Observers.Returns(_observers);
        _namespaceStorage.Recommendations.Returns(_recommendations);
        _observers.Get(Arg.Any<ObserverId>()).Returns(_ => new ObserverState());
        _recommendations.GetAll().Returns(_ => _rows.ToImmutableList());
        _recommendations.Remove(Arg.Any<RecommendationId>()).Returns(call =>
        {
            _removed.Add(call.Arg<RecommendationId>());
            return Task.CompletedTask;
        });
    }

    protected RecommendationState Replay(string subject, bool ignored = false) => new()
    {
        Id = RecommendationId.New(),
        Occurred = _now.AddHours(-2),
        IsIgnored = ignored,
        Request = new ReplayCandidateRequest { ObserverId = subject }
    };

    protected RecommendationState Registration(string subject, bool ignored = false) => new()
    {
        Id = RecommendationId.New(),
        Occurred = _now.AddHours(-2),
        IsIgnored = ignored,
        Request = new IgnoredProjectionRegistrationRequest { ProjectionId = subject }
    };

    protected async Task Prune() => _count = await OrphanedRecommendations.Prune(_store, _namespaceStorage, _now.AddHours(-1));
}
