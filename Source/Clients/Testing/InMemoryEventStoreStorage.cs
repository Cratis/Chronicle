// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Testing.EventSequences;
using KernelConcept = KernelConcepts::Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreStorage"/> for testing.
/// </summary>
/// <param name="eventStore">The <see cref="KernelConcept.EventStoreName"/> this storage serves.</param>
/// <param name="eventSequenceStorage">The <see cref="InMemoryEventSequenceStorage"/> for the event sequence under test.</param>
/// <param name="uniqueConstraintsStorage">Optional <see cref="InMemoryUniqueConstraintsStorage"/> for unique constraints.</param>
/// <param name="uniqueEventTypesStorage">Optional <see cref="InMemoryUniqueEventTypesConstraintsStorage"/> for unique event type constraints.</param>
/// <param name="constraintsStorage">Optional <see cref="InMemoryConstraintsStorage"/> with client constraint definitions.</param>
/// <param name="identityStorage">Optional <see cref="InMemoryIdentityStorage"/>.</param>
/// <param name="eventTypesStorage">Optional <see cref="InMemoryEventTypesStorage"/>.</param>
internal sealed class InMemoryEventStoreStorage(
    KernelConcept::EventStoreName eventStore,
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage? uniqueConstraintsStorage = null,
    InMemoryUniqueEventTypesConstraintsStorage? uniqueEventTypesStorage = null,
    InMemoryConstraintsStorage? constraintsStorage = null,
    InMemoryIdentityStorage? identityStorage = null,
    InMemoryEventTypesStorage? eventTypesStorage = null) : IEventStoreStorage
{
    /// <inheritdoc/>
    public KernelConcept::EventStoreName EventStore => eventStore;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes => eventTypesStorage ?? throw new NotSupportedException();

    /// <inheritdoc/>
    public IConstraintsStorage Constraints => constraintsStorage ?? throw new NotSupportedException();

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
            identityStorage);
}
