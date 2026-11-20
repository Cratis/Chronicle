// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Imports.for_Import.given;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval.given;

/// <summary>
/// Sets up an <see cref="UnseenObserverRemoval"/> over an event store with two namespaces, seeded by <see cref="Seed"/>.
/// </summary>
public abstract class an_unseen_observer_removal : Specification
{
    protected const string EventStore = "store";
    protected const string First = "first-namespace";
    protected const string Second = "second-namespace";

    protected readonly SettableTime _time = new() { Now = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero) };
    protected readonly Dictionary<string, IObserver> _grains = [];
    protected readonly Dictionary<EventStoreNamespaceName, Storage.InMemory.Observation.ObserverStateStorage> _states = [];
    protected readonly List<string> _removed = [];
    protected UnseenObserverRemoval _removal;
    protected ObserverRemovalResult _removalResult = ObserverRemovalResult.Removed;
    protected int _removeCalls;

    readonly Dictionary<EventStoreNamespaceName, IEventStoreNamespaceStorage> _namespaceStorages = [];
    readonly List<JobState> _jobs = [];
    readonly List<ObserverDefinition> _definitions = [];

    protected DateTimeOffset Old => _time.Now - TimeSpan.FromDays(8);

    protected DateTimeOffset Recent => _time.Now - TimeSpan.FromDays(1);

    protected virtual ChronicleOptions Configuration => new();

    protected virtual IDurableSubscriptionRecords? Records => null;

    async Task Establish()
    {
        foreach (var @namespace in new EventStoreNamespaceName[] { First, Second })
        {
            _states[@namespace] = new();
            var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
            namespaceStorage.Observers.Returns(_states[@namespace]);
            namespaceStorage.FailedPartitions.GetFor(Arg.Any<ObserverId?>()).Returns(new FailedPartitions());
            _namespaceStorages[@namespace] = namespaceStorage;
        }

        await Seed();

        var namespaces = Substitute.For<INamespaceStorage>();
        namespaces.GetAll().Returns([new NamespaceState(First, DateTimeOffset.UnixEpoch), new NamespaceState(Second, DateTimeOffset.UnixEpoch)]);
        var observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();
        observerDefinitions.GetAll().Returns(_definitions);
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.Namespaces.Returns(namespaces);
        eventStoreStorage.Observers.Returns(observerDefinitions);
        foreach (var (name, namespaceStorage) in _namespaceStorages)
        {
            eventStoreStorage.GetNamespace(name).Returns(namespaceStorage);
        }

        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns([(EventStoreName)EventStore]);
        storage.GetEventStore(EventStore).Returns(eventStoreStorage);

        var jobsManager = Substitute.For<IJobsManager>();
        jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(_jobs.ToImmutableList()));
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => _grains[call.ArgAt<string>(0)]);
        grainFactory.GetGrain<IJobsManager>(0, Arg.Any<string?>()).Returns(jobsManager);

        var remover = Substitute.For<IObserverRemover>();
        remover.Remove(Arg.Any<EventStoreName>(), Arg.Any<ObserverId>(), Arg.Any<EventSequenceId>()).Returns(call =>
        {
            _removeCalls++;
            if (_removalResult.Outcome == ObserverRemovalOutcome.Removed)
            {
                _removed.Add(call.ArgAt<ObserverId>(1).Value);
            }

            return Task.FromResult(_removalResult);
        });

        _removal = new(
            storage,
            grainFactory,
            remover,
            Options.Create(Configuration),
            NullLogger<UnseenObserverRemoval>.Instance,
            Substitute.For<IMeter<UnseenObserverRemoval>>(),
            _time,
            Records);
    }

    void Destroy() => _removal.Dispose();

    /// <summary>
    /// Seeds the observers of the event store, through <see cref="Observer"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected abstract Task Seed();

    protected async Task Observer(
        string id,
        DateTimeOffset? lastSeenInFirst,
        DateTimeOffset? lastSeenInSecond,
        bool isSubscribed = false,
        ObserverRunningState runningState = ObserverRunningState.Disconnected,
        bool hasFailedPartition = false,
        bool hasJob = false,
        ObserverOwner owner = ObserverOwner.Client,
        ObserverType type = ObserverType.Reactor)
    {
        _definitions.Add(new ObserverDefinition(id, [], EventSequenceId.Log, type, owner, true));
        var seen = new Dictionary<EventStoreNamespaceName, DateTimeOffset?> { [First] = lastSeenInFirst, [Second] = lastSeenInSecond };
        foreach (var @namespace in new EventStoreNamespaceName[] { First, Second })
        {
            await _states[@namespace].Save(new KernelObserverState { Identifier = id, RunningState = runningState }, EventSequenceId.Log);
            if (seen[@namespace] is { } value)
            {
                await _states[@namespace].SaveLastSeenAt(id, value, EventSequenceId.Log);
            }

            var observer = Substitute.For<IObserver>();
            observer.IsSubscribed().Returns(isSubscribed);
            observer.GetState().Returns(new KernelObserverState { Identifier = id, RunningState = runningState });
            _grains[new ObserverKey(id, EventStore, @namespace, EventSequenceId.Log)] = observer;

            if (hasFailedPartition)
            {
                _namespaceStorages[@namespace].FailedPartitions.GetFor((ObserverId?)id).Returns(
                    new FailedPartitions { Partitions = [new FailedPartition { ObserverId = id }] });
            }
        }

        if (hasJob)
        {
            var request = Substitute.For<IObserverJobRequest>();
            request.ObserverKey.Returns(new ObserverKey(id, EventStore, First, EventSequenceId.Log));
            _jobs.Add(new JobState { Id = JobId.New(), Type = new JobType("job"), Status = JobStatus.Running, Request = request });
        }
    }
}
