// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

/// <summary>
/// A subscription that is already registered and already subscribed is ready, and saying so takes no waiting -
/// so a caller that allows no time to wait must still be told it is ready rather than that it timed out.
/// </summary>
/// <remarks>
/// Both phases of the wait used to test the deadline before doing any work, so an exhausted budget meant zero
/// looks at the state and zero readiness checks - the caller got a timeout describing a subscription nobody had
/// examined. The same shape is what made a slow machine fail the wait even when the subscription had arrived:
/// the registration poll would spend the budget, and the readiness check it then skipped was the one that
/// would have succeeded.
/// </remarks>
public class and_the_wait_has_no_time_left : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    IObserver _observer;
    Exception _error;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(Substitute.For<ILocalSiloDetails>());

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                [new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First)]));
    }

    async Task Because() =>
        _error = await Catch.Exception(() =>
            _manager.WaitUntilSubscribed(new EventStoreSubscriptionId(SourceEventStore), TimeSpan.Zero));

    [Fact] void should_not_throw() => _error.ShouldBeNull();

    [Fact] void should_have_asked_whether_it_was_subscribed() => _observer.Received().IsSubscribed();
}
