// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Changes;

namespace Cratis.Chronicle.Storage.InMemory.Changes;

/// <summary>
/// Represents an in-memory implementation of <see cref="IChangesetStorage"/>.
/// </summary>
/// <remarks>
/// This is an audit/debug log of changesets that have been applied to read models. It keeps the
/// saved changesets in memory keyed by read model, and clears them when a replay begins.
/// </remarks>
public sealed class ChangesetStorage : IChangesetStorage
{
    readonly ConcurrentDictionary<ReadModelContainerName, List<StoredChangeset>> _changesetsByReadModel = new();

    /// <inheritdoc/>
    public Task BeginReplay(ReadModelContainerName readModel)
    {
        _changesetsByReadModel.TryRemove(readModel, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndReplay(ReadModelContainerName readModel) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Save(
        ReadModelContainerName readModel,
        Key readModelKey,
        EventType eventType,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var changesets = _changesetsByReadModel.GetOrAdd(readModel, _ => []);
        var stored = new StoredChangeset(readModelKey, eventType, sequenceNumber, correlationId, changeset.Changes);
        lock (changesets)
        {
            changesets.Add(stored);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Represents a stored changeset entry for a read model.
    /// </summary>
    /// <param name="ReadModelKey">The <see cref="Key"/> for the read model instance.</param>
    /// <param name="EventType">The <see cref="EventType"/> that was at the root of the change.</param>
    /// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/>.</param>
    /// <param name="CorrelationId">The <see cref="CorrelationId"/> the changeset was saved for.</param>
    /// <param name="Changes">The associated changes.</param>
    sealed record StoredChangeset(
        Key ReadModelKey,
        EventType EventType,
        EventSequenceNumber SequenceNumber,
        CorrelationId CorrelationId,
        IEnumerable<Change> Changes);
}
