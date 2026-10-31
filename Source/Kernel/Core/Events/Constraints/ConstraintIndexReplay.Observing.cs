// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    /// <summary>
    /// Gets whether another storage call succeeded during this attempt.
    /// </summary>
    internal bool StorageCallSucceeded { get; private set; }

    /// <summary>
    /// Gets whether a failure occurred while reading or collecting, before writing this segment.
    /// </summary>
    internal bool IsFolding => _phase is ConstraintIndexReplayPhase.Reading or ConstraintIndexReplayPhase.Collecting;

    /// <summary>
    /// Gets the constraint whose storage write has not returned yet.
    /// </summary>
    internal ConstraintName? WritingConstraint { get; private set; }

    /// <summary>
    /// Gets or sets the provider policy used to distinguish corrupt cursor input from transient failures.
    /// </summary>
    internal IStorageErrorClassifier? StorageErrorClassifier { get; set; }

    /// <summary>
    /// Seeds settlements retained by the activation when an earlier replay was discarded.
    /// </summary>
    internal IEnumerable<(ConstraintName Name, EventSequenceNumber Position)> SettledPoison
    {
        init => _poison.UnionWith(value);
    }

    /// <summary>
    /// Refolds without a poison event's partial staged effects, preserving preceding valid events.
    /// </summary>
    /// <param name="name">The failed constraint.</param>
    /// <param name="position">The failed event.</param>
    internal void SettlePoison(ConstraintName name, EventSequenceNumber position)
    {
        if (!_poison.Add((name, position))) return;
        _settledWithError++;
        if (CatchUp is { Refusing: true } barrier) barrier.UnderRefusal = true;
        _unique.Clear();
        _closed.Clear();
        _differences.Clear();
        _segment = 0;
        _remainingCatchUpSegments = null;
        _catchUpGroups = null;
        _catchUpGroupIndex = 0;
        _needsRetryPass = false;
        _retryPass = false;
        _overflow = false;
        BeginSegment();
    }

    bool IsPoison(ConstraintName name, EventSequenceNumber position) => _poison.Contains((name, position));

    async Task<bool> MoveNext(IEventCursor cursor, CancellationToken cancellationToken)
    {
        _operationConstraint = null;
        _operationPosition = null;
        return await cursor.MoveNext().WaitAsync(cancellationToken);
    }

    void SettleError(IConstraintDefinition definition, AppendedEvent @event, Exception error)
    {
        var position = @event.Context.SequenceNumber;
        if (_segment != 0 || _retryPass || (_lastSettledError.TryGetValue(definition.Name, out var last) && position <= last)) return;
        _lastSettledError[definition.Name] = position;
        _settledWithError++;
        if (@event.Context.EventType.Id == GlobalEventTypes.Redaction && error is InvalidClosingStreamScope && definition is ClosesStreamConstraintDefinition { EventStreamIdFrom: not null })
        {
            logger.UnrecoverableRedactedScope(definition.Name, position, error);
        }
        else
        {
            logger.UnresolvedReplayEvent(definition.Name, position, error);
        }
    }
}
