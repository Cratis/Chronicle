// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;
using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_service_has_two_replicas.and_the_observers_are_quiet.context;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_service_has_two_replicas;

/// <summary>
/// Two replicas of a service register the same reactor, the second with another set of event types, so the observer keeps the
/// first as a standby. While both replicas are connected and quiet the observer of the replicated reactor is released like the
/// one of a reactor that has a single replica (the control).
/// </summary>
/// <param name="context">The kernel with two replicas of the reactor connected.</param>
[Collection(given.DormantObserverCollection.Name)]
public class and_the_observers_are_quiet(context context) : Given<context>(context)
{
    public class context(given.DormantObserverFixture fixture) : given.a_dormant_reactor(fixture)
    {
        OwnedClient? _replica;

        public bool ControlResident;
        public bool ReplicatedObserverResident;

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

            await _store.EventLog.Append("source", new given.DormantObserverEvent("first"));
            await WaitForHandled(1);
        }

        async Task Because()
        {
            // The control has a single replica; once it is released the replicated observer has had the same time to follow.
            await WaitUntilReleased(_store.Reactors.GetHandlerFor<given.DormantAlphaReactor>());
            await Task.Delay(TimeSpan.FromSeconds(10));
            ControlResident = (await ResidentObserverGrains(_store.Reactors.GetHandlerFor<given.DormantAlphaReactor>())).Any();
            ReplicatedObserverResident = await ObserverIsResident();
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

    [Fact] void should_keep_the_observer_resident_while_it_holds_a_standby() => Context.ReplicatedObserverResident.ShouldBeTrue();
}
