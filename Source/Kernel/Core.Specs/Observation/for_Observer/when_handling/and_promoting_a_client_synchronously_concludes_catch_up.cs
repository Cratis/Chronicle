// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_promoting_a_client_synchronously_concludes_catch_up : given.an_observer_with_a_superseded_client
{
    bool _acknowledged;

    void Establish()
    {
        _connectedClients.IsConnected(_supersededClient.ConnectionId).Returns(true);
        var subscriber = Substitute.For<IClientOwnedObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Disconnected());
        _silo.AddProbe(_ => subscriber);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 42UL, LastHandledEventSequenceNumber = 41UL };

        // Promotion recovers the interrupted delivery through a partition job before routing observer-wide work.
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(async _ =>
            {
                await _observer.CaughtUp(JobId.NotSet, 41UL, 41UL);
                _acknowledged = true;
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
    }

    async Task Because() => await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(active_event_type, 42UL)])
        .WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_accept_the_handover_without_waiting_on_its_own_delivery() => _acknowledged.ShouldBeTrue();
    [Fact] void should_not_advance_past_the_disconnected_delivery() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
