// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_quarantined_after_the_recovery_hint : given.an_event_sequence_with_reconciled_capture
{
    bool _passedHint;

    async Task Establish()
    {
        await SubscribeCapture();
        _registeredTypes = [_eventType, new EventType("new-event", EventTypeGeneration.First)];
        var observer = Substitute.For<IObserver>();
        _captureGrainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
        observer.NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>()).Returns(async call =>
        {
            _passedHint = await _captureObserver.NeedsSubscriptionRecovery(call.Arg<IEnumerable<EventType>>());

            // Quarantine wins the next observer turn after the caller receives a positive hint.
            await _captureObserver.TransitionTo<QuarantinedObserver>();
            _captureState.ClearReceivedCalls();
            _appendedEventsQueues.ClearReceivedCalls();

            return _passedHint;
        });
        observer.RecoverStalledSubscription<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false)
            .Returns(call => _captureObserver.RecoverStalledSubscription<IPatternCaptureSubscriber>(ObserverType.Reactor, call.Arg<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false));
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_have_passed_the_hint() => _passedHint.ShouldBeTrue();
    [Fact] async Task should_not_rewrite_the_matching_definition() => await _captureDefinitions.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
    [Fact] async Task should_keep_the_quarantine() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_update_the_subscription() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_eventType);
    [Fact] async Task should_not_write_recovery_state() => await _captureState.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_resubscribe_to_the_queue() => await _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
