// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Alerts;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Benchmarks.Observer;

/// <summary>
/// Minimal test-kit activation of the real observer with in-memory persistent state and substituted boundaries.
/// </summary>
public class ObserverFixture
{
    /// <summary>
    /// The event type used by both paths.
    /// </summary>
    public static readonly EventType EventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    readonly TestKitSilo _silo = new();
    readonly List<object> _substitutes = [];
    IStorage<ObserverState> _state = null!;

    /// <summary>
    /// Gets the real observer activation.
    /// </summary>
    public Chronicle.Observation.Observer Observer { get; private set; } = null!;

    /// <summary>
    /// Gets the current observer state.
    /// </summary>
    public ObserverState State => _state.State;

    /// <summary>
    /// Activates an observer using the same test-kit boundaries as the observer specifications.
    /// </summary>
    /// <returns>The activation task.</returns>
    public async Task Setup()
    {
        ObserverId id = "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
        var key = new ObserverKey(id, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
        var configuration = SubstituteFor<IConfigurationForObserverProvider>();
        configuration.GetFor(Arg.Any<string>()).Returns(new Observers { WriteCoalescingInterval = 0 });
        var options = SubstituteFor<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions());
        var storage = SubstituteFor<Chronicle.Storage.IStorage>();
        var eventStore = SubstituteFor<IEventStoreStorage>();
        var eventNamespace = SubstituteFor<IEventStoreNamespaceStorage>();
        var inFlight = SubstituteFor<IInFlightEventsStorage>();
        var handledCounts = SubstituteFor<IObserverHandledCountsStorage>();
        var eventTypes = SubstituteFor<IEventTypesStorage>();
        var eventSequenceStorage = SubstituteFor<IEventSequenceStorage>();
        var jobStorage = SubstituteFor<IJobStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.EventTypes.Returns(eventTypes);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventNamespace);
        eventNamespace.InFlightEvents.Returns(inFlight);
        inFlight.GetFor(Arg.Any<ObserverId>()).Returns([]);
        eventNamespace.ObserverHandledCounts.Returns(handledCounts);
        eventNamespace.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(eventSequenceStorage);
        eventNamespace.Jobs.Returns(jobStorage);
        handledCounts.GetFor(Arg.Any<ObserverId>(), Arg.Any<Key>()).Returns(new Dictionary<EventTypeId, EventCount>());
        eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([]);
        jobStorage.GetJob(Arg.Any<JobId>()).Returns(call => Task.FromResult<Catch<JobState, Cratis.Orleans.Storage.Jobs.JobError>>(new JobState
        {
            Id = call.Arg<JobId>(),
            Status = JobStatus.Running
        }));
        jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
        jobStorage.GetOutstandingJobs().Returns(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));

        var compliance = SubstituteFor<IEventCompliance>();
        compliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        var subscriber = SubstituteFor<IObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
        var jobs = SubstituteFor<IJobsManager>();
        jobs.GetJobs(Arg.Any<JobQuery>()).Returns(ImmutableList<JobState>.Empty);
        jobs.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(_ => Result<JobId, StartJobError>.Success(JobId.New()));
        var alerts = SubstituteFor<IObserverAlerts>();
        alerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call =>
        {
            var snapshot = call.Arg<ObserverAlertSnapshot>();
            return new ObserverAlertReceipt(snapshot.LifecycleId, snapshot.Revision, ObserverAlertReconciliation.Applied);
        });
        var sequence = SubstituteFor<Chronicle.EventSequences.IEventSequence>();
        sequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        sequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);
        var queues = SubstituteFor<Chronicle.EventSequences.IAppendedEventsQueues>();
        queues.Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(_ => new Chronicle.EventSequences.AppendedEventsQueueSubscription(key, 0));

        _silo.AddService(configuration);
        _silo.AddService(options);
        _silo.AddService(storage);
        _silo.AddService(compliance);
        _silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        _silo.AddService(SubstituteFor<IWebhookTargetPolicy>());
        _silo.AddService(SubstituteFor<IEventGenerationRelease>());
        _silo.AddService(SubstituteFor<IObserverServiceClient>());
        _silo.AddService(TimeProvider.System);
        _silo.AddService<ILogger<Chronicle.Observation.Observer>>(NullLogger<Chronicle.Observation.Observer>.Instance);
        _silo.AddService<ILoggerFactory>(NullLoggerFactory.Instance);
        _silo.AddKeyedService<IActivitySource<Chronicle.Observation.Observer>>(WellKnown.MeterName, new ActivitySource<Chronicle.Observation.Observer>());
        _silo.AddProbe(_ => alerts);
        _silo.AddProbe(_ => subscriber);
        _silo.AddProbe(_ => jobs);
        _silo.AddProbe(_ => sequence);
        _silo.AddProbe(_ => queues);

        _state = _silo.StorageManager.GetStorage<ObserverState>(typeof(Chronicle.Observation.Observer).FullName!);
        var definition = _silo.StorageManager.GetStorage<ObserverDefinition>(nameof(ObserverDefinition));
        definition.State = new ObserverDefinition { Identifier = id, EventTypes = [EventType], IsReplayable = true };
        await definition.WriteStateAsync();
        var failures = _silo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        failures.State = new FailedPartitions();
        eventNamespace.Observers.Get(id).Returns(_ => _state.State with { Identifier = id });
        eventNamespace.Observers.Save(Arg.Any<ObserverState>(), Arg.Any<EventSequenceId>()).Returns(true);
        eventNamespace.FailedPartitions.GetFor(id).Returns(_ => failures.State);
        Observer = await _silo.CreateGrainAsync<Chronicle.Observation.Observer>(key);
        await Observer.Subscribe<IObserverSubscriber>(ObserverType.Reactor, [EventType], SiloAddress.Zero);
        ClearCalls();
    }

    /// <summary>
    /// Resets only the live cursor to reuse preallocated input without measuring harness state copies.
    /// </summary>
    public void ResetCursor() => _state.State = _state.State with { NextEventSequenceNumber = 0UL, LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable };

    /// <summary>
    /// Releases NSubstitute invocation records between iterations.
    /// </summary>
    public void ClearCalls()
    {
        foreach (var substitute in _substitutes)
        {
            substitute.ClearReceivedCalls();
        }
    }

    T SubstituteFor<T>()
        where T : class
    {
        var substitute = Substitute.For<T>();
        _substitutes.Add(substitute);
        return substitute;
    }
}
