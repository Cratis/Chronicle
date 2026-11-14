// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Subscribing takes a settled observer through CatchingUpInFlight and Routing on its way back to Observing.
/// Its reported running state should remain Active throughout that recovery cycle.
/// </summary>
/// <remarks>
/// Samples read the current state-storage instance during collaborator calls inside each phase. They do not
/// establish when durable writes occur or what external subscribers receive.
/// </remarks>
public class and_the_observer_has_already_settled : given.an_observer_with_subscription
{
    Key _inFlightPartition;
    bool _hasEnteredCatchingUpInFlight;
    List<ObserverRunningState> _reportedWhileCatchingUpInFlight;
    List<ObserverRunningState> _reportedWhileRouting;
    List<ObserverRunningState> _reportedWhileObserving;

    async Task Establish()
    {
        _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
        (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();

        _inFlightPartition = "some-partition";
        _reportedWhileCatchingUpInFlight = [];
        _reportedWhileRouting = [];
        _reportedWhileObserving = [];
        _stateStorage.State.InFlightPartitions.Add(_inFlightPartition);

        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _jobsManager
            .When(_ => _.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
            .Do(_ =>
            {
                _hasEnteredCatchingUpInFlight = true;
                _reportedWhileCatchingUpInFlight.Add(_stateStorage.State.RunningState);
            });

        // Subscribe also looks up the next event before recovery, so only sample lookups after catch-up starts.
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

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Initial=Active/Observing; Catchup({_reportedWhileCatchingUpInFlight.Count})=[{string.Join(", ", _reportedWhileCatchingUpInFlight)}]; " +
            $"Routing({_reportedWhileRouting.Count})=[{string.Join(", ", _reportedWhileRouting)}]; " +
            $"Subscribe({_reportedWhileObserving.Count})=[{string.Join(", ", _reportedWhileObserving)}]; " +
            $"Final={_stateStorage.State.RunningState}/{(await _observer.GetCurrentState()).GetType().Name}");
    }

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
