// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_versioned_owner_on_another_sequence : an_observer_with_a_transport_on_another_sequence
{
    protected IRecommendationsManager _recommendations;

    void Establish()
    {
        _client = _client with { ClientType = ".NET", Version = "2.0.0" };
        _owningObserver.GetSubscription().Returns(new ObserverSubscription(_observerId, _owningKey, [], typeof(NullObserverSubscriber), SiloAddress.Zero)
        {
            Targets = [new(SiloAddress.Zero, new ConnectedClient { ClientType = ".NET", Version = "1.0.0" })]
        });
        _eventStoreStorage.Namespaces.GetAll().Returns([new(_observerKey.Namespace, DateTimeOffset.UtcNow)]);
        _eventStoreNamespaceStorage.Observers.HandOver(
            _observerId,
            Arg.Any<EventSequenceId?>(),
            Arg.Any<ObserverSequenceCheckpoint>()).Returns(true);
        _recommendations = Substitute.For<IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendations);
    }

    protected override ILogger<Observer> CreateLogger() => new RecordingObserverLogger();

    protected Task Register() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero, _client);

    protected void ShouldNotRecommend() => _recommendations.ReceivedCalls().ShouldBeEmpty();

    protected Task ShouldRecommendAnUnorderedSwitch() => _recommendations.Received(1).Add<IObserverRegistrationConflictRecommendation, ObserverRegistrationConflictRequest>(
        Arg.Is<RecommendationDescription>(description => description.Value.Contains("Cratis:Chronicle:ApplicationVersion") && description.Value.Contains("Cratis__Chronicle__ApplicationVersion")),
        Arg.Is<ObserverRegistrationConflictRequest>(request => request.ObserverId == _observerId && request.IncomingEventSequenceId == _observerKey.EventSequenceId &&
            request.OwnerEventSequenceId == _definitionEventSequenceId && request.IncomingVersion == _client.Version && request.OwnerVersion == "1.0.0" && request.Outcome == "unordered"));
}
