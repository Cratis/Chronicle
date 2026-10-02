// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_a_capture_observer : an_event_sequence
{
    protected Observer _captureObserver;
    protected IStorage<ObserverState> _captureState;
    protected IPatternCaptureSubscriber _captureSubscriber;
    protected FailedPartitions _captureFailures;

    async Task Establish()
    {
        var observerSilo = new TestKitSilo();
        _captureState = Substitute.For<IStorage<ObserverState>>();
        _captureState.State = ObserverState.Empty with
        {
            Identifier = PatternCapture.ObserverIdentifier,
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = 43UL
        };
        observerSilo.Options.StorageFactory = type => type == typeof(ObserverState)
            ? _captureState
            : null!; // Let TestKit use its default storage for definitions and failures.
        var configuration = Substitute.For<IConfigurationForObserverProvider>();
        configuration.GetFor(Arg.Any<string>()).Returns(new Observers());
        observerSilo.AddService(configuration);
        observerSilo.AddService(_storage);
        var compliance = Substitute.For<IEventCompliance>();
        compliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, Concepts.EventTypes.EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        observerSilo.AddService(compliance);
        observerSilo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        observerSilo.AddService<ILogger<Observer>>(NullLogger<Observer>.Instance);
        observerSilo.AddService<ILoggerFactory>(NullLoggerFactory.Instance);
        observerSilo.AddKeyedService<IMeter<Observer>>(WellKnown.MeterName, Substitute.For<IMeter<Observer>>());
        observerSilo.AddKeyedService<IActivitySource<Observer>>(WellKnown.MeterName, new ActivitySource<Observer>());
        observerSilo.AddProbe<IEventSequence>(_ => _eventSequence);
        observerSilo.AddProbe(_ => _jobsManager);
        observerSilo.AddProbe(_ => _appendedEventsQueues);
        _captureSubscriber = Substitute.For<IPatternCaptureSubscriber>();
        observerSilo.AddProbe(_ => _captureSubscriber);
        _captureSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(43UL));
        _appendedEventsQueues.Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(call => new AppendedEventsQueueSubscription(call.Arg<ObserverKey>(), 0));
        var definition = observerSilo.StorageManager.GetStorage<ObserverDefinition>(nameof(ObserverDefinition));
        definition.State = new ObserverDefinition { Identifier = PatternCapture.ObserverIdentifier, IsReplayable = false };
        var failures = observerSilo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        _captureFailures = new FailedPartitions();
        failures.State = _captureFailures;
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, EventStore, EventStoreNamespace, _eventSequenceKey.EventSequenceId);
        _captureObserver = await observerSilo.CreateGrainAsync<Observer>(key);
        _silo.AddProbe<IObserver>(_ => _captureObserver);
        _patternCapture.Subscribe(EventStore, EventStoreNamespace).Returns(_ => SubscribeCapture());
        _patternCapture.RecoverSubscription(EventStore, EventStoreNamespace).Returns(_ => RecoverCapture());
    }

    protected Task SubscribeCapture() => _captureObserver.Subscribe<IPatternCaptureSubscriber>(
        ObserverType.Reactor, [_eventType], SiloAddress.Zero, isReplayable: false);

    protected Task RecoverCapture() => _captureObserver.RecoverStalledSubscription<IPatternCaptureSubscriber>(
        ObserverType.Reactor, [_eventType], SiloAddress.Zero, isReplayable: false);
}
