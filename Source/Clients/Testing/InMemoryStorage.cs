// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Testing.EventSequences;
using KernelConcept = KernelConcepts::Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an in-memory implementation of <see cref="IStorage"/> for testing.
/// </summary>
/// <param name="eventSequenceStorage">The <see cref="InMemoryEventSequenceStorage"/> for the event sequence under test.</param>
/// <param name="uniqueConstraintsStorage">Optional <see cref="InMemoryUniqueConstraintsStorage"/> for unique constraints.</param>
/// <param name="uniqueEventTypesStorage">Optional <see cref="InMemoryUniqueEventTypesConstraintsStorage"/> for unique event type constraints.</param>
/// <param name="constraintsStorage">Optional <see cref="InMemoryConstraintsStorage"/> with client constraint definitions.</param>
/// <param name="identityStorage">Optional <see cref="InMemoryIdentityStorage"/>.</param>
/// <param name="eventTypesStorage">Optional <see cref="InMemoryEventTypesStorage"/>.</param>
internal sealed class InMemoryStorage(
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage? uniqueConstraintsStorage = null,
    InMemoryUniqueEventTypesConstraintsStorage? uniqueEventTypesStorage = null,
    InMemoryConstraintsStorage? constraintsStorage = null,
    InMemoryIdentityStorage? identityStorage = null,
    InMemoryEventTypesStorage? eventTypesStorage = null) : IStorage
{
    /// <inheritdoc/>
    public ISystemStorage System => throw new NotSupportedException();

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
            eventTypesStorage);
}
