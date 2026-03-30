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
internal class NullKernelStorage : IStorage, IEventStoreStorage, IEventStoreNamespaceStorage
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullKernelStorage"/>.
    /// </summary>
    public static readonly NullKernelStorage Instance = new();

    // IStorage
    ISystemStorage IStorage.System => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    Task<IEnumerable<KernelConceptsNs::EventStoreName>> IStorage.GetEventStores() => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    Task<bool> IStorage.HasEventStore(KernelConceptsNs::EventStoreName eventStore) => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    ISubject<IEnumerable<KernelConceptsNs::EventStoreName>> IStorage.ObserveEventStores() => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventStoreStorage IStorage.GetEventStore(KernelConceptsNs::EventStoreName eventStore) => this;

    // IEventStoreStorage
    KernelConceptsNs::EventStoreName IEventStoreStorage.EventStore => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    INamespaceStorage IEventStoreStorage.Namespaces => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventTypesStorage IEventStoreStorage.EventTypes => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IConstraintsStorage IEventStoreStorage.Constraints => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IObserverDefinitionsStorage IEventStoreStorage.Observers => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IReactorDefinitionsStorage IEventStoreStorage.Reactors => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IReducerDefinitionsStorage IEventStoreStorage.Reducers => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IProjectionDefinitionsStorage IEventStoreStorage.Projections => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IWebhookDefinitionsStorage IEventStoreStorage.Webhooks => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventStoreSubscriptionDefinitionsStorage IEventStoreStorage.EventStoreSubscriptions => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IReadModelDefinitionsStorage IEventStoreStorage.ReadModels => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventSeedingStorage IEventStoreStorage.EventSeeding => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventStoreNamespaceStorage IEventStoreStorage.GetNamespace(KernelConceptsNs::EventStoreNamespaceName @namespace) => this;

    // IEventStoreNamespaceStorage
    IChangesetStorage IEventStoreNamespaceStorage.Changesets => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IIdentityStorage IEventStoreNamespaceStorage.Identities => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IJobStorage IEventStoreNamespaceStorage.Jobs => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IJobStepStorage IEventStoreNamespaceStorage.JobSteps => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IObserverStateStorage IEventStoreNamespaceStorage.Observers => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IFailedPartitionsStorage IEventStoreNamespaceStorage.FailedPartitions => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IRecommendationStorage IEventStoreNamespaceStorage.Recommendations => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IObserverKeyIndexes IEventStoreNamespaceStorage.ObserverKeyIndexes => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IReplayContexts IEventStoreNamespaceStorage.ReplayContexts => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    ISinks IEventStoreNamespaceStorage.Sinks => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IReplayedReadModelsStorage IEventStoreNamespaceStorage.ReplayedReadModels => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventSeedingStorage IEventStoreNamespaceStorage.EventSeeding => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IProjectionFuturesStorage IEventStoreNamespaceStorage.ProjectionFutures => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IEventSequenceStorage IEventStoreNamespaceStorage.GetEventSequence(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) => NullEventSequenceStorage.Instance;
    IUniqueConstraintsStorage IEventStoreNamespaceStorage.GetUniqueConstraintsStorage(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) => throw new NotSupportedException("NullKernelStorage does not support this operation.");
    IUniqueEventTypesConstraintsStorage IEventStoreNamespaceStorage.GetUniqueEventTypesConstraints(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId eventSequenceId) => throw new NotSupportedException("NullKernelStorage does not support this operation.");
}
