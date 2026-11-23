// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;
using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_service_has_two_replicas.and_the_observers_are_quiet.context;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_service_has_two_replicas;

/// <summary>
/// Two replicas of a service register the same reactor, the second with another set of event types, so the observer keeps the
/// first as a standby. While both replicas are connected and quiet the observer of the replicated reactor is released like the
/// one of a reactor that has a single replica (the control). When the replica that holds the subscription leaves, the next event
/// wakes the observer and the standby takes over, delivering it exactly once.
/// </summary>
/// <param name="context">The kernel with two replicas of the reactor connected.</param>
[Collection(given.DormantObserverCollection.Name)]
public class and_the_observers_are_quiet(context context) : Given<context>(context)
{
    public class context(given.DormantObserverFixture fixture) : given.a_dormant_reactor<given.DormantObserverFixture>(fixture)
    {
        OwnedClient? _replica;

        public bool ControlResident;
        public bool ReplicatedObserverResident;
        public EventSequenceNumber[] HandledByTheReplica;
        public EventSequenceNumber[] HandledByTheStandby;
        public EventSequenceNumber SecondEvent;

        public given.DormantReplicaReactor ReplicaReactor { get; } = new();

        async Task Establish()
        {
            _replica = await OwnedClient.Start(
                Services,
                ChronicleClient.Options.DefaultSinkTypeId,
                _store.Name.Value,
                _store.Namespace.Value,
                [typeof(given.DormantObserverEvent), typeof(given.DormantGammaEvent)],
                ReplicaReactor);

            await WaitUntilTheReplicaHoldsTheSubscription();
            await _store.EventLog.Append("source", new given.DormantObserverEvent("first"));
            await WaitForHandled(ReplicaReactor.Handled, 1);
        }

        /// <summary>
        /// The replica registers the definition that has one event type more; it is the one that holds the subscription once the
        /// observer has it, and the first client is then the standby.
        /// </summary>
        /// <returns>A task that completes when the replica holds the subscription.</returns>
        /// <exception cref="TimeoutException">The replica never took over the subscription.</exception>
        async Task WaitUntilTheReplicaHoldsTheSubscription()
        {
            var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<Cratis.Chronicle.Observation.IObserver>(new Concepts.Observation.ObserverKey(_handler.Id.Value, _store.Name.Value, Concepts.EventStoreNamespaceName.Default, Concepts.EventSequences.EventSequenceId.Log));
            var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
            while ((await observer.GetSubscription()).EventTypes.Count < 2)
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    throw new TimeoutException("The replica never took over the subscription");
                }

                // Task.Delay: poll interval while waiting for the replica to take over, bounded by the deadline.
                await Task.Delay(250);
            }
        }

        async Task Because()
        {
            // The control has a single replica; once it is released the replicated observer has had the same time to follow.
            await WaitUntilReleased(_store.Reactors.GetHandlerFor<given.DormantAlphaReactor>());
            await WaitUntilReleased();
            ControlResident = (await ResidentObserverGrains(_store.Reactors.GetHandlerFor<given.DormantAlphaReactor>())).Any();
            ReplicatedObserverResident = await ObserverIsResident();
            HandledByTheReplica = [.. ReplicaReactor.Handled];

            // The replica that holds the subscription leaves while the observer is dormant: the standby takes over for the next event.
            await _replica!.DisposeAsync();
            _replica = null;
            var appended = await _store.EventLog.Append("source", new given.DormantObserverEvent("second"));
            SecondEvent = appended.SequenceNumber;
            await WaitForHandled(1);
            // Task.Delay: proving an absence; the standby must not handle the event, so there is no fact to await.
            await Task.Delay(TimeSpan.FromSeconds(3));
            HandledByTheStandby = [.. Reactor.Handled];
        }

        async Task Destroy()
        {
            if (_replica is not null)
            {
                await _replica.DisposeAsync();
            }
        }
    }

    [Fact] void should_have_released_the_observer_of_the_reactor_with_a_single_replica() => Context.ControlResident.ShouldBeFalse();
    [Fact] void should_release_the_observer_that_holds_a_standby() => Context.ReplicatedObserverResident.ShouldBeFalse();
    [Fact] void should_have_delivered_the_first_event_to_the_replica_holding_the_subscription() => Context.HandledByTheReplica.Length.ShouldEqual(1);
    [Fact] void should_deliver_the_next_event_to_the_standby() => Context.HandledByTheStandby.ShouldContainOnly(Context.SecondEvent);
}
