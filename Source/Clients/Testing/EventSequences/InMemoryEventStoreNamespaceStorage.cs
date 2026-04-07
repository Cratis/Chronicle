// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.Sinks;
using KernelEventSequences = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreNamespaceStorage"/>.
/// </summary>
/// <param name="eventSequenceStorage">The <see cref="InMemoryEventSequenceStorage"/> for the event sequence under test.</param>
/// <param name="uniqueConstraintsStorage">The <see cref="InMemoryUniqueConstraintsStorage"/> for unique constraints.</param>
/// <param name="uniqueEventTypesStorage">The <see cref="InMemoryUniqueEventTypesConstraintsStorage"/> for unique event type constraints.</param>
/// <param name="identityStorage">The <see cref="InMemoryIdentityStorage"/>.</param>
sealed class InMemoryEventStoreNamespaceStorage(
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage uniqueConstraintsStorage,
    InMemoryUniqueEventTypesConstraintsStorage uniqueEventTypesStorage,
    InMemoryIdentityStorage identityStorage) : IEventStoreNamespaceStorage
{
    /// <inheritdoc/>
    public IChangesetStorage Changesets => throw new NotSupportedException();

    /// <inheritdoc/>
    public IIdentityStorage Identities => identityStorage;

    /// <inheritdoc/>
    public IJobStorage Jobs => throw new NotSupportedException();

    /// <inheritdoc/>
    public IJobStepStorage JobSteps => throw new NotSupportedException();

    /// <inheritdoc/>
    public IObserverStateStorage Observers => throw new NotSupportedException();

    /// <inheritdoc/>
    public IFailedPartitionsStorage FailedPartitions => throw new NotSupportedException();

    /// <inheritdoc/>
    public IRecommendationStorage Recommendations => throw new NotSupportedException();

    /// <inheritdoc/>
    public IObserverKeyIndexes ObserverKeyIndexes => throw new NotSupportedException();

    /// <inheritdoc/>
    public IReplayContexts ReplayContexts => throw new NotSupportedException();

    /// <inheritdoc/>
    public ISinks Sinks => throw new NotSupportedException();

    /// <inheritdoc/>
    public IReplayedReadModelsStorage ReplayedReadModels => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventSeedingStorage EventSeeding => throw new NotSupportedException();

    /// <inheritdoc/>
    public IProjectionFuturesStorage ProjectionFutures => throw new NotSupportedException("Projection futures storage is not needed for in-memory event sequence testing.");

    /// <inheritdoc/>
    public IEventSequenceStorage GetEventSequence(KernelEventSequences::EventSequenceId eventSequenceId) => eventSequenceStorage;

    /// <inheritdoc/>
    public IUniqueConstraintsStorage GetUniqueConstraintsStorage(KernelEventSequences::EventSequenceId eventSequenceId) => uniqueConstraintsStorage;

    /// <inheritdoc/>
    public IUniqueEventTypesConstraintsStorage GetUniqueEventTypesConstraints(KernelEventSequences::EventSequenceId eventSequenceId) => uniqueEventTypesStorage;
}
