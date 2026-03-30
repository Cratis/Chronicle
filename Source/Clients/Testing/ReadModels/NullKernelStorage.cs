// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

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
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a null implementation of <see cref="IStorage"/> used for read model context testing.
/// </summary>
/// <remarks>
/// Only the minimal storage chain needed by the projection engine is implemented.
/// All other operations throw <see cref="NotSupportedException"/>.
/// </remarks>
internal sealed class NullKernelStorage : IStorage, IEventStoreStorage, IEventStoreNamespaceStorage
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullKernelStorage"/>.
    /// </summary>
    public static readonly NullKernelStorage Instance = new();

    /// <inheritdoc/>
    ISystemStorage IStorage.System => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    KernelConceptsNs::EventStoreName IEventStoreStorage.EventStore => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    INamespaceStorage IEventStoreStorage.Namespaces => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IEventTypesStorage IEventStoreStorage.EventTypes => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IConstraintsStorage IEventStoreStorage.Constraints => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IObserverDefinitionsStorage IEventStoreStorage.Observers => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IReactorDefinitionsStorage IEventStoreStorage.Reactors => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IReducerDefinitionsStorage IEventStoreStorage.Reducers => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IProjectionDefinitionsStorage IEventStoreStorage.Projections => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IWebhookDefinitionsStorage IEventStoreStorage.Webhooks => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IEventStoreSubscriptionDefinitionsStorage IEventStoreStorage.EventStoreSubscriptions => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IReadModelDefinitionsStorage IEventStoreStorage.ReadModels => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IEventSeedingStorage IEventStoreStorage.EventSeeding => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IChangesetStorage IEventStoreNamespaceStorage.Changesets => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IIdentityStorage IEventStoreNamespaceStorage.Identities => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IJobStorage IEventStoreNamespaceStorage.Jobs => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IJobStepStorage IEventStoreNamespaceStorage.JobSteps => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IObserverStateStorage IEventStoreNamespaceStorage.Observers => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IFailedPartitionsStorage IEventStoreNamespaceStorage.FailedPartitions => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IRecommendationStorage IEventStoreNamespaceStorage.Recommendations => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IObserverKeyIndexes IEventStoreNamespaceStorage.ObserverKeyIndexes => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IReplayContexts IEventStoreNamespaceStorage.ReplayContexts => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    ISinks IEventStoreNamespaceStorage.Sinks => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IReplayedReadModelsStorage IEventStoreNamespaceStorage.ReplayedReadModels => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IEventSeedingStorage IEventStoreNamespaceStorage.EventSeeding => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IProjectionFuturesStorage IEventStoreNamespaceStorage.ProjectionFutures => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    Task<IEnumerable<KernelConceptsNs::EventStoreName>> IStorage.GetEventStores() => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    Task<bool> IStorage.HasEventStore(KernelConceptsNs::EventStoreName eventStore) => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    ISubject<IEnumerable<KernelConceptsNs::EventStoreName>> IStorage.ObserveEventStores() => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IEventStoreStorage IStorage.GetEventStore(KernelConceptsNs::EventStoreName eventStore) => this;

    /// <inheritdoc/>
    IEventStoreNamespaceStorage IEventStoreStorage.GetNamespace(KernelConceptsNs::EventStoreNamespaceName @namespace) => this;

    /// <inheritdoc/>
    IEventSequenceStorage IEventStoreNamespaceStorage.GetEventSequence(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) => NullEventSequenceStorage.Instance;

    /// <inheritdoc/>
    IUniqueConstraintsStorage IEventStoreNamespaceStorage.GetUniqueConstraintsStorage(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) => throw new NotSupportedException("NullKernelStorage does not support this operation.");

    /// <inheritdoc/>
    IUniqueEventTypesConstraintsStorage IEventStoreNamespaceStorage.GetUniqueEventTypesConstraints(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) => throw new NotSupportedException("NullKernelStorage does not support this operation.");
}
