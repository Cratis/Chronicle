// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant.given;

/// <summary>
/// The observer of an event store subscription forwarding to event store "Target": subscribed, observing, and in a cluster
/// that supports dormant observers.
/// </summary>
public class a_subscribed_event_store_subscription_observer : a_subscribed_client_observer
{
    protected const string TargetEventStore = "Target";

    protected IEventStoreSubscriptionObserverSubscriber _subscriber;
    protected List<ObserverSubscriberContext> _contexts = [];

    protected override ObserverId _observerId => EventStoreSubscriptionObserverIds.For("Source", TargetEventStore);

    protected override void ConfigureProbes(TestKitSilo silo)
    {
        base.ConfigureProbes(silo);
        _subscriber = Substitute.For<IEventStoreSubscriptionObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(callInfo =>
        {
            _contexts.Add(callInfo.Arg<ObserverSubscriberContext>());
            var events = callInfo.Arg<IEnumerable<Concepts.Events.AppendedEvent>>().ToArray();
            return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
        });
        silo.AddProbe(_ => _subscriber);
    }

    async Task Establish()
    {
        await _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, [event_type], SiloAddress.Zero, TargetEventStore, automatic: true);
        await _silo.TimerRegistry.FireAllAsync();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }
}
