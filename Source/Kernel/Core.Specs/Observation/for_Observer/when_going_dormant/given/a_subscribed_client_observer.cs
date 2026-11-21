// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant.given;

/// <summary>
/// A client reactor observer that is subscribed, observing, and in a cluster that supports dormant observers.
/// </summary>
public class a_subscribed_client_observer : for_Observer.given.an_observer_that_lost_its_client
{
    protected static readonly TimeSpan _dormantPeriod = TimeSpan.FromSeconds(900);
    protected readonly DeliveryClock _clock = new();
    protected IConnectedClients _connectedClients;

    public a_subscribed_client_observer() => _timeProvider = _clock;

    protected override bool DormantFloorIsReached => true;

    protected virtual int DormantAfter => (int)_dormantPeriod.TotalSeconds;

    protected virtual DurableSubscriptionMode Mode => DurableSubscriptionMode.Release;

    protected int ReleaseRequests => _silo.GrainRuntime.Mock.Invocations.Count(invocation => invocation.Method.Name == nameof(IGrainRuntime.DeactivateOnIdle));

    protected override Observers CreateObserversConfig() => new()
    {
        WriteCoalescingInterval = 0,
        IdleDeactivationAfter = 0,
        DormantAfter = DormantAfter,
        DurableSubscriptions = Mode
    };

    protected ObserverDurableSubscription? Recorded => _stateStorage.State.DurableSubscription;

    protected IReactorObserverSubscriber _reactor;

    protected override void ConfigureProbes(TestKitSilo silo)
    {
        silo.AddProbe(_ => _connectedClients);
        silo.AddProbe(_ => _reactor);
    }

    async Task Establish()
    {
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(true);
        _connectedClients.GetConnectedClient(Arg.Any<ConnectionId>()).Returns(_client);
        _appendedEventsQueues.TryMarkDormant(Arg.Any<ObserverKey>()).Returns(true);
        await _observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _eventSequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);

        // Subscribing owes one sweep of failed partitions and retry jobs, run by a turn of its own.
        await _silo.TimerRegistry.FireAllAsync();

        // The alert reminder a new subscription registers is released by the tick that finds nothing left to report.
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }

    protected async Task RunWatchdogAfter(TimeSpan elapsed)
    {
        _clock.Advance(elapsed);
        await _observer.RunWatchdogAsync();
    }

    protected Task Deactivate() =>
        _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Dormant"), default);
}
