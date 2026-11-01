// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Keys;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverKeyIndex"/> that reads event sources from events.
/// </summary>
/// <param name="eventSequence">The event sequence to read.</param>
/// <param name="scope">The reader scope to enumerate keys for.</param>
public sealed class ObserverKeyIndex(EventSequenceStorage eventSequence, ObserverReaderScope scope) : IObserverKeyIndex
{
    /// <inheritdoc/>
    public IObserverKeys GetKeys(EventSequenceNumber fromEventSequenceNumber) =>
        new ObserverKeys(eventSequence, scope, fromEventSequenceNumber);

    /// <inheritdoc/>
    public Task Add(Key key) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Rebuild() => Task.CompletedTask;

    /// <summary>
    /// Represents keys read lazily from an in-memory event sequence.
    /// </summary>
    /// <param name="eventSequence">The event sequence to read when enumerated.</param>
    /// <param name="scope">The reader scope to enumerate keys for.</param>
    /// <param name="fromEventSequenceNumber">The inclusive position to read from.</param>
    sealed class ObserverKeys(
        EventSequenceStorage eventSequence,
        ObserverReaderScope scope,
        EventSequenceNumber fromEventSequenceNumber) : IObserverKeys
    {
        /// <inheritdoc/>
        public async IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var eventTypes = scope.EventTypes.Select(_ => _.Id).ToHashSet();
            var eventSourceIds = eventSequence.Events
                .Where(_ => _.Context.SequenceNumber >= fromEventSequenceNumber &&
                    (scope.IsWildcard || eventTypes.Contains(_.Context.EventType.Id)))
                .Select(_ => _.Context.EventSourceId)
                .Distinct();
            foreach (var eventSourceId in eventSourceIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new Key(eventSourceId.Value, ArrayIndexers.NoIndexers);
            }

            await Task.CompletedTask;
        }
    }
}
