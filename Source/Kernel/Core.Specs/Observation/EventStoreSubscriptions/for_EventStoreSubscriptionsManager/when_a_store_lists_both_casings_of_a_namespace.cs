// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.given;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager;

public class when_a_store_lists_both_casings_of_a_namespace : a_subscriber_with_events
{
    readonly List<string> _observerKeys = [];
    TestKitSilo _managerSilo;
    IObserver _observer;
    Exception? _activationError;
    bool _subscribed;

    protected override EventStoreName SourceEventStore => "Studio";
    protected override EventStoreNamespaceName SourceNamespace => "Cratis";

    void Establish()
    {
        var eventType = new EventType("source-event", EventTypeGeneration.First);
        _events = [new(EventContext.From("Studio", "Cratis", eventType, EventSourceType.Default, "source-1", EventStreamType.All, EventStreamId.Default, 42UL, CorrelationId.New()), new ExpandoObject())];
        _namespaceCasing.Canonicalize("Lobby", "Cratis").Returns((EventStoreNamespaceName)"Cratis");

        _managerSilo = new TestKitSilo();
        _managerSilo.AddService(Substitute.For<ILocalSiloDetails>());
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([(EventStoreNamespaceName)"cratis", (EventStoreNamespaceName)"Cratis"]);
        namespaces.GetCanonical(Arg.Any<EventStoreNamespaceName>()).Returns((EventStoreNamespaceName)"Cratis");
        _managerSilo.AddProbe(_ => namespaces);
        _managerSilo.AddService<INamespaceCasing>(new NamespaceCasing(_managerSilo.GrainFactory, NullLogger<NamespaceCasing>.Instance));

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(_ => _subscribed);
        _observer.GetEventTypes().Returns([eventType]);
        _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>(),
            true,
            automatic: true).Returns(async _ =>
            {
                await Forward();
                _subscribed = true;
            });
        _managerSilo.AddProbe<IObserver>(identity =>
        {
            _observerKeys.Add(identity.ToString());
            return _observer;
        });
        _managerSilo.StorageManager.GetStorage<EventStoreSubscriptionsState>(typeof(EventStoreSubscriptionsManager).FullName!).State = new()
        {
            Subscriptions = [new(new EventStoreSubscriptionId("Studio"), "Studio", [eventType])]
        };
    }

    async Task Because() => _activationError = await Catch.Exception(async () =>
    {
        var manager = await _managerSilo.CreateGrainAsync<EventStoreSubscriptionsManager>("Lobby");
        await manager.Ensure();
    });

    [Fact] void should_complete_activation_and_the_startup_ensure() => _activationError.ShouldBeNull();
    [Fact] void should_activate_exactly_one_canonical_observer() => _observerKeys.ShouldContainOnly(new ObserverKey("Studio->Lobby", "Studio", "Cratis", EventSequenceId.Outbox).ToString());
    [Fact] void should_never_activate_the_lowercase_observer() => _observerKeys.Exists(_ => _.Contains("#cratis#", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact]
    void should_subscribe_the_live_observer_once() => _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
        ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), true, automatic: true);
    [Fact] void should_forward_the_single_source_event_once() => AppendCalls().ShouldEqual(1);
    [Fact] void should_use_the_canonical_target_inbox() => _inboxKey.ShouldEqual(new EventSequenceKey("inbox-Studio", "Lobby", "Cratis").ToString());
}
