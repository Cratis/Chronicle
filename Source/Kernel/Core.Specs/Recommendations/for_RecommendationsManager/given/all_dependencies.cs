// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.given;

public class all_dependencies : Specification
{
    protected TestKitSilo _silo = new();
    protected RecommendationsManager _manager;
    protected RecommendationsManagerKey _managerKey;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IRecommendationStorage _recommendationStorage;
    protected ITheRecommendation _theRecommendation;

    protected List<RecommendationState> _storedRecommendations;

    async Task Establish()
    {
        _storedRecommendations = [];
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _recommendationStorage = Substitute.For<IRecommendationStorage>();
        _theRecommendation = Substitute.For<ITheRecommendation>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Recommendations.Returns(_recommendationStorage);

        _recommendationStorage.GetAll().Returns(_ => Task.FromResult<IImmutableList<RecommendationState>>([.. _storedRecommendations]));

        _silo.AddService(_storage);
        _silo.AddProbe<ITheRecommendation>(_ => _theRecommendation);

        _managerKey = new("event-store", "namespace");
        _manager = await _silo.CreateGrainAsync<RecommendationsManager>(0, _managerKey);
    }
}
