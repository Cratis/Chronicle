// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Keys;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverKeyIndexes"/>.
/// </summary>
/// <param name="eventSequences">Provider for the event sequence an observer reads.</param>
public sealed class ObserverKeyIndexes(Func<EventSequenceId, EventSequenceStorage> eventSequences) : IObserverKeyIndexes
{
    /// <inheritdoc/>
    public Task<IObserverKeyIndex> GetFor(ObserverKey observerKey, ObserverReaderScope scope) =>
        Task.FromResult<IObserverKeyIndex>(new ObserverKeyIndex(eventSequences(observerKey.EventSequenceId), scope));
}
