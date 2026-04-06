// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.Sinks;
using KernelConcept = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelEventSequences = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IStorage"/> that provides all storage
/// components needed by the kernel <c>EventSequence</c> grain during testing.
/// </summary>
/// <param name="eventSequenceStorage">The <see cref="InMemoryEventSequenceStorage"/> for the event sequence under test.</param>
/// <param name="uniqueConstraintsStorage">The <see cref="InMemoryUniqueConstraintsStorage"/> for unique constraints.</param>
/// <param name="uniqueEventTypesStorage">The <see cref="InMemoryUniqueEventTypesConstraintsStorage"/> for unique event type constraints.</param>
/// <param name="constraintsStorage">The <see cref="InMemoryConstraintsStorage"/> with client constraint definitions.</param>
/// <param name="identityStorage">The <see cref="InMemoryIdentityStorage"/>.</param>
/// <param name="eventTypesStorage">The <see cref="InMemoryEventTypesStorage"/>.</param>
/// <param name="eventSequenceId">The <see cref="KernelEventSequences::EventSequenceId"/> for the event sequence under test.</param>
internal sealed class InMemoryKernelStorage(
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage uniqueConstraintsStorage,
    InMemoryUniqueEventTypesConstraintsStorage uniqueEventTypesStorage,
    InMemoryConstraintsStorage constraintsStorage,
    InMemoryIdentityStorage identityStorage,
    InMemoryEventTypesStorage eventTypesStorage,
    KernelEventSequences::EventSequenceId eventSequenceId) : IStorage
{
    /// <inheritdoc/>
    public ISystemStorage System => throw new NotSupportedException("System storage is not needed for in-memory event sequence testing.");

    /// <inheritdoc/>
    public Task<IEnumerable<KernelConcept::EventStoreName>> GetEventStores() =>
        Task.FromResult(Enumerable.Empty<KernelConcept::EventStoreName>());

    /// <inheritdoc/>
    public Task<bool> HasEventStore(KernelConcept::EventStoreName eventStore) => Task.FromResult(true);

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelConcept::EventStoreName>> ObserveEventStores() =>
        new Subject<IEnumerable<KernelConcept::EventStoreName>>();

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(KernelConcept::EventStoreName eventStore) =>
        new InMemoryEventStoreStorage(
            eventStore,
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            constraintsStorage,
            identityStorage,
            eventTypesStorage,
            eventSequenceId);
}

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreStorage"/>.
/// </summary>
sealed class InMemoryEventStoreStorage(
    KernelConcept::EventStoreName eventStore,
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage uniqueConstraintsStorage,
    InMemoryUniqueEventTypesConstraintsStorage uniqueEventTypesStorage,
    InMemoryConstraintsStorage constraintsStorage,
    InMemoryIdentityStorage identityStorage,
    InMemoryEventTypesStorage eventTypesStorage,
    KernelEventSequences::EventSequenceId eventSequenceId) : IEventStoreStorage
{
    /// <inheritdoc/>
    public KernelConcept::EventStoreName EventStore => eventStore;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes => eventTypesStorage;

    /// <inheritdoc/>
    public IConstraintsStorage Constraints => constraintsStorage;

    /// <inheritdoc/>
    public IObserverDefinitionsStorage Observers => throw new NotSupportedException();

    /// <inheritdoc/>
    public IReactorDefinitionsStorage Reactors => throw new NotSupportedException();

    /// <inheritdoc/>
    public IReducerDefinitionsStorage Reducers => throw new NotSupportedException();

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections => throw new NotSupportedException();

    /// <inheritdoc/>
    public IWebhookDefinitionsStorage Webhooks => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventStoreSubscriptionDefinitionsStorage EventStoreSubscriptions => throw new NotSupportedException();

    /// <inheritdoc/>
    public IReadModelDefinitionsStorage ReadModels => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventSeedingStorage EventSeeding => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(KernelConcept::EventStoreNamespaceName @namespace) =>
        new InMemoryEventStoreNamespaceStorage(
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            identityStorage,
            eventSequenceId);
}

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreNamespaceStorage"/>.
/// </summary>
sealed class InMemoryEventStoreNamespaceStorage(
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage uniqueConstraintsStorage,
    InMemoryUniqueEventTypesConstraintsStorage uniqueEventTypesStorage,
    InMemoryIdentityStorage identityStorage,
    KernelEventSequences::EventSequenceId eventSequenceId) : IEventStoreNamespaceStorage
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
    public IEventSequenceStorage GetEventSequence(KernelEventSequences::EventSequenceId _) => eventSequenceStorage;

    /// <inheritdoc/>
    public IUniqueConstraintsStorage GetUniqueConstraintsStorage(KernelEventSequences::EventSequenceId _) => uniqueConstraintsStorage;

    /// <inheritdoc/>
    public IUniqueEventTypesConstraintsStorage GetUniqueEventTypesConstraints(KernelEventSequences::EventSequenceId _) => uniqueEventTypesStorage;
}
