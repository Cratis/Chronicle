// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationDetails.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected Subject<IEnumerable<RecommendationState>> _source;
    protected RecommendationState _active;
    protected RecommendationState _ignored;
    protected List<IEnumerable<RecommendationDetails>> _activeEmissions;
    protected List<IEnumerable<RecommendationDetails>> _ignoredEmissions;

    void Establish()
    {
        _source = new();
        _activeEmissions = [];
        _ignoredEmissions = [];
        _active = new RecommendationState { Id = Guid.NewGuid() };
        _ignored = new RecommendationState { Id = Guid.NewGuid(), IsIgnored = true };

        _storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var recommendationStorage = Substitute.For<IRecommendationStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        namespaceStorage.Recommendations.Returns(recommendationStorage);
        recommendationStorage.ObserveRecommendations().Returns(_source);

        RecommendationDetails.AllRecommendations("event-store", "namespace", _storage).Subscribe(_activeEmissions.Add);
        RecommendationDetails.IgnoredRecommendations("event-store", "namespace", _storage).Subscribe(_ignoredEmissions.Add);
    }

    protected static Guid[] Ids(IEnumerable<RecommendationDetails> details) => [.. details.Select(_ => _.Id)];
}
