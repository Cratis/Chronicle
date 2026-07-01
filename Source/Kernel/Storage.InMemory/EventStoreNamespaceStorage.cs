// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.InMemory.Changes;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;
using Cratis.Chronicle.Storage.InMemory.Jobs;
using Cratis.Chronicle.Storage.InMemory.Keys;
using Cratis.Chronicle.Storage.InMemory.Observation;
using Cratis.Chronicle.Storage.InMemory.Projections;
using Cratis.Chronicle.Storage.InMemory.Recommendations;
using Cratis.Chronicle.Storage.InMemory.Seeding;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.Sinks;

using InMemoryReadModels = Cratis.Chronicle.Storage.InMemory.ReadModels;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreNamespaceStorage"/>.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage serves.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage serves.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> for resolving job state types.</param>
/// <param name="sinks">The <see cref="ISinks"/> for the namespace.</param>
public sealed class EventStoreNamespaceStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IJobTypes jobTypes,
    ISinks sinks) : IEventStoreNamespaceStorage
{
    readonly ConcurrentDictionary<EventSequenceId, EventSequenceStorage> _eventSequences = new();
    readonly ConcurrentDictionary<EventSequenceId, IUniqueConstraintsStorage> _uniqueConstraints = new();
    readonly ConcurrentDictionary<EventSequenceId, IUniqueEventTypesConstraintsStorage> _uniqueEventTypesConstraints = new();
    readonly ConcurrentDictionary<EventSequenceId, IClosedStreamsConstraintStorage> _closedStreamsConstraints = new();

    /// <inheritdoc/>
    public IChangesetStorage Changesets { get; } = new ChangesetStorage();

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; } = new IdentityStorage();

    /// <inheritdoc/>
    public IJobStorage Jobs { get; } = new JobStorage(jobTypes);

    /// <inheritdoc/>
    public IJobStepStorage JobSteps { get; } = new JobStepStorage();

    /// <inheritdoc/>
    public IObserverStateStorage Observers { get; } = new ObserverStateStorage();

    /// <inheritdoc/>
    public IFailedPartitionsStorage FailedPartitions { get; } = new FailedPartitionStorage();

    /// <inheritdoc/>
    public IInFlightEventsStorage InFlightEvents { get; } = new InFlightEventsStorage();

    /// <inheritdoc/>
    public IRecommendationStorage Recommendations { get; } = new RecommendationStorage();

    /// <inheritdoc/>
    public IObserverKeyIndexes ObserverKeyIndexes { get; } = new ObserverKeyIndexes();

    /// <inheritdoc/>
    public ISinks Sinks { get; } = sinks;

    /// <inheritdoc/>
    public IReplayContexts ReplayContexts { get; } = new InMemoryReadModels.ReplayContexts();

    /// <inheritdoc/>
    public IReplayedReadModelsStorage ReplayedReadModels { get; } = new InMemoryReadModels.ReplayedReadModelsStorage();

    /// <inheritdoc/>
    public IEventSeedingStorage EventSeeding { get; } = new EventSeedingStorage();

    /// <inheritdoc/>
    public IProjectionFuturesStorage ProjectionFutures { get; } = new ProjectionFuturesStorage();

    /// <inheritdoc/>
    public IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId) => GetConcreteEventSequence(eventSequenceId);

    /// <inheritdoc/>
    public IUniqueConstraintsStorage GetUniqueConstraintsStorage(EventSequenceId eventSequenceId) =>
        _uniqueConstraints.GetOrAdd(eventSequenceId, _ => new UniqueConstraintsStorage());

    /// <inheritdoc/>
    public IUniqueEventTypesConstraintsStorage GetUniqueEventTypesConstraints(EventSequenceId eventSequenceId)
    {
        if (_uniqueEventTypesConstraints.TryGetValue(eventSequenceId, out var existing))
        {
            return existing;
        }

        var created = new UniqueEventTypesConstraintsStorage(GetConcreteEventSequence(eventSequenceId));
        return _uniqueEventTypesConstraints.GetOrAdd(eventSequenceId, created);
    }

    /// <inheritdoc/>
    public IClosedStreamsConstraintStorage GetClosedStreamsConstraints(EventSequenceId eventSequenceId) =>
        _closedStreamsConstraints.GetOrAdd(eventSequenceId, _ => new ClosedStreamsConstraintStorage());

    EventSequenceStorage GetConcreteEventSequence(EventSequenceId eventSequenceId)
    {
        if (_eventSequences.TryGetValue(eventSequenceId, out var existing))
        {
            return existing;
        }

        var created = new EventSequenceStorage(eventStore, @namespace, eventSequenceId);
        return _eventSequences.GetOrAdd(eventSequenceId, created);
    }
}
