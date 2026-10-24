// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Json;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_restorable_replay_job_step : Specification
{
    protected static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new EnumerableConceptAsJsonConverterFactory(), new ConceptAsJsonConverterFactory() }
    };

    protected readonly JobStepId _stepId = JobStepId.New();
    protected readonly JobStepKey _stepKey = new(JobId.New(), "event-store", "event-store-namespace");
    protected readonly AppendedEvent[] _corpus = [.. Enumerable.Range(1, 8).Select(number =>
        AppendedEvent.EmptyWithEventSequenceNumber((ulong)number) with
        {
            Context = EventContext.Empty with { SequenceNumber = (ulong)number, EventSourceId = number % 2 == 0 ? "even" : "odd" }
        })];

    protected static HandleEventsForObserverState InitialState() => new()
    {
        ObserverKey = new("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
        StartEventSequenceNumber = EventSequenceNumber.First,
        EndEventSequenceNumber = 8UL,
        EventObservationState = EventObservationState.Replay,
        ReaderScope = ObserverReaderScope.Wildcard
    };

    protected static HandleEventsForObserverState Restore(byte[] bytes) =>
        JsonSerializer.Deserialize<HandleEventsForObserverState>(bytes, SerializerOptions)!;

    protected async Task<Activation> CreateActivation(HandleEventsForObserverState state, SortedDictionary<ulong, string> sink, bool interrupt = false)
    {
        var activation = new Activation(state);
        var eventSequence = Substitute.For<IEventSequenceStorage>();
        var eventTypes = Substitute.For<IEventTypesStorage>();
        var eventStore = Substitute.For<Storage.IEventStoreStorage>();
        var eventNamespace = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventNamespace);
        eventNamespace.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(eventSequence);
        eventStore.EventTypes.Returns(eventTypes);
        eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));
        ConfigureCursor(activation, eventSequence, interrupt);
        var observer = Substitute.For<IObserver>();
        var subscription = new ObserverSubscription("observer-id", state.ObserverKey, [], typeof(IPageObserverType), SiloAddress.Zero);
        observer.GetSubscription().Returns(subscription);
        var subscriber = Substitute.For<IPageObserverType>();
        subscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
            foreach (var @event in events)
            {
                var number = @event.Context.SequenceNumber.Value;
                activation.Deliveries.Add(number);
                sink[number] = $"{number}:{@event.Context.EventSourceId.Value}";
            }
            return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
        });
        activation.Silo.AddProbe(_ => observer);
        activation.Silo.AddProbe(_ => subscriber);
        activation.Silo.AddService(storage);
        ConfigureServices(activation.Silo);
        var persistent = activation.Silo.AddPersistentStateStorage<HandleEventsForObserverState>(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);
        persistent.State = state;
        activation.Step = await activation.Silo.CreateGrainAsync<TestableHandleEventsForObserver>(_stepId, _stepKey);
        activation.Step.SetupForTesting(observer, subscription);
        activation.Step.GuardStateAccess = true;
        activation.Step.SnapshotState = value => activation.Snapshots.Add(JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions));
        return activation;
    }

    void ConfigureCursor(Activation activation, IEventSequenceStorage storage, bool interrupt)
    {
        var cursor = Substitute.For<IEventCursor>();
        AppendedEvent[][] pages = [];
        var page = -1;
        cursor.Current.Returns(_ => pages[page]);
        cursor.MoveNext().Returns(async _ =>
        {
            if (++page == 1 && interrupt)
            {
                activation.AtInterruption.SetResult();
                await activation.ReleaseInterruption.Task;
                throw new InvalidOperationException("Interrupted cursor after the unconfirmed suffix");
            }
            return page < pages.Length;
        });
        storage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                activation.RequestedStart = call.ArgAt<EventSequenceNumber>(0);
                var end = call.ArgAt<EventSequenceNumber>(1);
                pages = _corpus.Where(@event => @event.Context.SequenceNumber >= activation.RequestedStart && @event.Context.SequenceNumber <= end).Chunk(5).ToArray();
                return Task.FromResult(cursor);
            });
    }

    static void ConfigureServices(TestKitSilo silo)
    {
        silo.AddService<TimeProvider>(TimeProvider.System);
        var client = Substitute.For<IObserverServiceClient>();
        client.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] }));
        silo.AddService(client);
        silo.AddService(Substitute.For<IJobStepThrottle>());
        silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        var configuration = Substitute.For<IConfigurationForObserverProvider>();
        configuration.GetFor(Arg.Any<string>()).Returns(new Observers());
        silo.AddService(configuration);
        var compliance = Substitute.For<IEventCompliance>();
        compliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>()).Returns(call => Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        silo.AddService(compliance);
        silo.AddService(Substitute.For<IEventGenerationRelease>());
        var logger = silo.AddService(NullLogger<HandleEventsForObserver>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        silo.AddService(loggerFactory);
        silo.AddService(Options.Create(new JobsOptions { StepCheckpointBatchInterval = 100, StepCheckpointFlushInterval = TimeSpan.Zero }));
        silo.AddService(Options.Create(new ChronicleOptions
        {
            Jobs = new() { ReplayCheckpointEventInterval = 3, StepCheckpointBatchInterval = 100, StepCheckpointFlushInterval = TimeSpan.Zero }
        }));
    }

    protected sealed class Activation(HandleEventsForObserverState state)
    {
        public TestKitSilo Silo { get; } = new();
        public HandleEventsForObserverState State { get; } = state;
        public TestableHandleEventsForObserver Step { get; set; } = null!;
        public List<byte[]> Snapshots { get; } = [];
        public List<ulong> Deliveries { get; } = [];
        public EventSequenceNumber RequestedStart { get; set; } = EventSequenceNumber.Unavailable;
        public TaskCompletionSource AtInterruption { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseInterruption { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
