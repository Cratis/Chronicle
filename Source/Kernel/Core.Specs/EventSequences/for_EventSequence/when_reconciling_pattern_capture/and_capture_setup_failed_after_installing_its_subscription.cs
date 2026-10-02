// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_setup_failed_after_installing_its_subscription : given.an_event_sequence
{
    Observer _captureObserver;
    Exception _initialError;
    bool _initiallySubscribed;
    ObserverRunningState _initialRunningState;
    int _setupAttempts;

    async Task Establish()
    {
        var observerSilo = new TestKitSilo();
        var configuration = Substitute.For<IConfigurationForObserverProvider>();
        configuration.GetFor(Arg.Any<string>()).Returns(new Observers());
        observerSilo.AddService(configuration);
        observerSilo.AddService(_storage);
        observerSilo.AddService(Substitute.For<IEventCompliance>());
        observerSilo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        observerSilo.AddService<ILogger<Observer>>(NullLogger<Observer>.Instance);
        observerSilo.AddService<ILoggerFactory>(NullLoggerFactory.Instance);
        observerSilo.AddKeyedService<IMeter<Observer>>(WellKnown.MeterName, Substitute.For<IMeter<Observer>>());
        observerSilo.AddKeyedService<IActivitySource<Observer>>(WellKnown.MeterName, new ActivitySource<Observer>());
        observerSilo.AddProbe<IEventSequence>(_ => _eventSequence);
        observerSilo.AddProbe(_ => _jobsManager);
        observerSilo.AddProbe(_ => _appendedEventsQueues);
        _appendedEventsQueues.Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(call => new AppendedEventsQueueSubscription(call.Arg<ObserverKey>(), 0));
        var observerState = observerSilo.StorageManager.GetStorage<ObserverState>(typeof(Observer).FullName!);
        observerState.State = ObserverState.Empty with { LastHandledEventSequenceNumber = 42UL, NextEventSequenceNumber = 43UL };
        var definition = observerSilo.StorageManager.GetStorage<ObserverDefinition>(nameof(ObserverDefinition));
        definition.State = new ObserverDefinition { Identifier = PatternCapture.ObserverIdentifier, IsReplayable = false };
        var failures = observerSilo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        failures.State = new FailedPartitions();
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, EventStore, EventStoreNamespace, _eventSequenceKey.EventSequenceId);
        _captureObserver = await observerSilo.CreateGrainAsync<Observer>(key);
        _silo.AddProbe<IObserver>(_ => _captureObserver);

        // ResumeJobs runs after the real Subscribe has installed its in-memory subscription and written state.
        _jobsManager.GetAllJobs().Returns(_ => ++_setupAttempts == 1
            ? Task.FromException<IImmutableList<JobState>>(new TimeoutException())
            : Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _patternCapture.Subscribe(EventStore, EventStoreNamespace).Returns(_ =>
            _captureObserver.Subscribe<IPatternCaptureSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero, isReplayable: false));
        _initialError = await Catch.Exception(() => _patternCapture.Subscribe(EventStore, EventStoreNamespace));
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
