// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage;
using KernelConcept = KernelConcepts::Cratis.Chronicle.Concepts;

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
internal sealed class InMemoryKernelStorage(
    InMemoryEventSequenceStorage eventSequenceStorage,
    InMemoryUniqueConstraintsStorage uniqueConstraintsStorage,
    InMemoryUniqueEventTypesConstraintsStorage uniqueEventTypesStorage,
    InMemoryConstraintsStorage constraintsStorage,
    InMemoryIdentityStorage identityStorage,
    InMemoryEventTypesStorage eventTypesStorage) : IStorage
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
            eventTypesStorage);
}
