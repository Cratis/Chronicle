// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_setup_failed_after_installing_its_subscription : given.an_event_sequence_with_a_capture_observer
{
    Exception _initialError;
    bool _initiallySubscribed;
    ObserverRunningState _initialRunningState;
    int _setupAttempts;

    async Task Establish()
    {
        // ResumeJobs runs after the real Subscribe has installed its in-memory subscription and written state.
        _jobsManager.GetAllJobs().Returns(_ => ++_setupAttempts == 1
            ? Task.FromException<IImmutableList<JobState>>(new TimeoutException())
            : Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _initialError = await Catch.Exception(SubscribeCapture);
        _initiallySubscribed = await _captureObserver.IsSubscribed();
        _initialRunningState = (await _captureObserver.GetState()).RunningState;
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_have_failed_during_real_observer_setup() => _initialError.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_have_installed_the_subscription_before_failing() => _initiallySubscribed.ShouldBeTrue();
    [Fact] void should_have_left_capture_disconnected() => _initialRunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_retry_setup_on_the_next_tick() => _setupAttempts.ShouldEqual(2);
    [Fact] async Task should_make_capture_active() => (await _captureObserver.GetState()).RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_preserve_captured_progress() => (await _captureObserver.GetState()).LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
