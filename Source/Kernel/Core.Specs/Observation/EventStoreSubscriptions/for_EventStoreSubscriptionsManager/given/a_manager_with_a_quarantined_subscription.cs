// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Moq;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.given;

public class a_manager_with_a_quarantined_subscription : Specification
{
    protected const string TargetEventStore = "Lobby";
    protected const string SourceEventStore = "StudioAdmin";
    protected const string ReminderName = "event-store-subscription-subscribe:StudioAdmin";

    protected TestKitSilo _silo;
    protected EventStoreSubscriptionsManager _manager;
    protected IObserver _observer;
    protected INamespaces _namespaces;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(Substitute.For<ILocalSiloDetails>());
        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => _namespaces);
        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(false);
        _observer.IsObserverQuarantined().Returns(true);
        _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>(),
            true).Returns(call =>
            {
                _observer.IsSubscribed().Returns(true);
                _observer.GetEventTypes().Returns(call.Arg<IEnumerable<EventType>>());
                return Task.CompletedTask;
            });
        _silo.AddProbe<IObserver>(identity => GetObserver(identity.ToString()));

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);
        await _manager.Add(new(new EventStoreSubscriptionId(SourceEventStore), new EventStoreName(SourceEventStore), [EventType.Unknown]));
        _observer.ClearReceivedCalls();
    }

    protected virtual IObserver GetObserver(string identity) => _observer;

    protected void ShouldNotSubscribe(IObserver observer) => observer.DidNotReceive()
        .Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>());

    protected void ShouldScheduleReminder(int count) => _silo.ReminderRegistry.Mock.Verify(_ =>
        _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), ReminderName, TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(1)),
        Times.Exactly(count));

    protected void ShouldSubscribe(IObserver observer) => observer.Received(1)
        .Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), TargetEventStore, Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), true);
}
