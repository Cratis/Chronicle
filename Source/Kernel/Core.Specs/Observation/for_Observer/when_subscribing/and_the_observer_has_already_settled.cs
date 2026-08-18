// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Subscribing takes a settled observer through CatchingUpInFlight and Routing on its way back to Observing.
/// Neither is a state the observer rests in and neither has a running state of its own, so the value the observer
/// reports must stay on the last settled one the whole way through. It used to be overwritten with Unknown on
/// entering each of them, and every transition writes the stored state before the transition it schedules from its
/// own OnEnter runs - so that Unknown reached everything watching the observer.
/// </summary>
/// <remarks>
/// The running state is sampled from the observer's stored state at points that only execute inside one of those
/// states. That stored state is the very instance GetState hands out and the very instance the write at the end of
/// each transition persists, so a sample taken inside a state is exactly what that state publishes.
/// </remarks>
public class and_the_observer_has_already_settled : given.an_observer_with_subscription
{
    Key _inFlightPartition;
    bool _hasEnteredCatchingUpInFlight;
    List<ObserverRunningState> _reportedWhileCatchingUpInFlight;
    List<ObserverRunningState> _reportedWhileRouting;
    List<ObserverRunningState> _reportedWhileObserving;

    void Establish()
    {
        _inFlightPartition = "some-partition";
        _reportedWhileCatchingUpInFlight = [];
        _reportedWhileRouting = [];
        _reportedWhileObserving = [];

        // An in-flight partition is what gives CatchingUpInFlight work to do, and starting its catch-up job is the
        // one thing it does that can be observed from outside.
        _stateStorage.State.InFlightPartitions.Add(_inFlightPartition);

        _jobsManager
            .When(_ => _.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
            .Do(_ =>
            {
                _hasEnteredCatchingUpInFlight = true;
                _reportedWhileCatchingUpInFlight.Add(_stateStorage.State.RunningState);
            });

        // Routing looks for the next event it has not handled on entry. Subscribing makes the same lookup before
        // any transition starts, so only the ones that come after CatchingUpInFlight - the state Routing is
        // entered from here - belong to Routing.
        _eventSequence
            .When(_ => _.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()))
            .Do(_ =>
            {
                if (_hasEnteredCatchingUpInFlight)
                {
                    _reportedWhileRouting.Add(_stateStorage.State.RunningState);
                }
            });

        _appendedEventsQueues
            .When(_ => _.Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>()))
            .Do(_ => _reportedWhileObserving.Add(_stateStorage.State.RunningState));
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] void should_pass_through_catching_up_in_flight() => _reportedWhileCatchingUpInFlight.ShouldNotBeEmpty();
    [Fact] void should_pass_through_routing() => _reportedWhileRouting.ShouldNotBeEmpty();
    [Fact] void should_settle_in_observing() => _reportedWhileObserving.ShouldNotBeEmpty();

    [Fact] void should_not_report_unknown_while_catching_up_in_flight() => _reportedWhileCatchingUpInFlight.ShouldNotContain(ObserverRunningState.Unknown);
    [Fact] void should_not_report_unknown_while_routing() => _reportedWhileRouting.ShouldNotContain(ObserverRunningState.Unknown);

    [Fact] void should_keep_reporting_active_while_catching_up_in_flight() => _reportedWhileCatchingUpInFlight.Distinct().ShouldContainOnly(ObserverRunningState.Active);
    [Fact] void should_keep_reporting_active_while_routing() => _reportedWhileRouting.Distinct().ShouldContainOnly(ObserverRunningState.Active);
    [Fact] void should_keep_reporting_active_while_observing() => _reportedWhileObserving.Distinct().ShouldContainOnly(ObserverRunningState.Active);

    [Fact] void should_be_active_when_it_has_settled_again() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
