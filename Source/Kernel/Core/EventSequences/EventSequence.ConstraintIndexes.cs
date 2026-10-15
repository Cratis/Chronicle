// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    readonly Dictionary<ConstraintName, IConstraintDefinition> _indexedDefinitions = [];
    readonly Dictionary<ConstraintName, EventSequenceNumber> _pendingConstraintIndexes = [];
    readonly Dictionary<ConstraintName, EventSequenceNumber> _closingCoverageStarts = [];
    readonly HashSet<ConstraintName> _abandonedConstraintIndexes = [];
    readonly HashSet<ConstraintName> _replayAbandonedIndexes = [];
    int _replaySettledErrors;
    EventSequenceNumber _constraintIndexPosition = EventSequenceNumber.First;
    ConstraintIndexReplay? _constraintIndexReplay;
    ConstraintIndexReplayScope[] _replayScopes = [];
    EventSequenceNumber _replayEndPoint = EventSequenceNumber.First;
    DateTimeOffset? _lastAppendRepairEnded;
    DateTimeOffset _nextConstraintRepairAttempt;
    int _constraintRepairFailures;

    /// <summary>
    /// Gets or sets the clock used for repair cadence and retry deadlines.
    /// </summary>
    internal TimeProvider ConstraintIndexTimeProvider { get; set; } = TimeProvider.System;

    void InitializeConstraintIndexes()
    {
        _indexedDefinitions.Clear();
        _pendingConstraintIndexes.Clear();
        _closingCoverageStarts.Clear();
        _abandonedConstraintIndexes.Clear();
        _constraintIndexReplay = null;
        _lastAppendRepairEnded = null;
        _nextConstraintRepairAttempt = default;
        _constraintRepairFailures = 0;

        // Legacy initialization is isolated here; historical catch-up can attach at this boundary.
        State.ConstraintIndexWatermark = State.ConstraintIndexWatermark is { } watermark && watermark < State.SequenceNumber ? watermark : State.SequenceNumber;
        _constraintIndexPosition = State.SequenceNumber;
        SynchronizeIndexedDefinitions(State.ConstraintIndexWatermark);
        if (State.ConstraintIndexWatermark < State.SequenceNumber)
        {
            MarkConstraintIndexGap(State.ConstraintIndexWatermark);
        }
    }

    void SynchronizeIndexedDefinitions(EventSequenceNumber? coverageStart = null)
    {
        var definitions = _constraints!.IndexedDefinitions.ToDictionary(definition => definition.Name);
        if (_indexedDefinitions.Count != definitions.Count || definitions.Any(pair => !_indexedDefinitions.TryGetValue(pair.Key, out var previous) || !pair.Value.Equals(previous)))
        {
            _constraintIndexReplay = null;
        }
        foreach (var name in _indexedDefinitions.Keys.Concat(_pendingConstraintIndexes.Keys).Except(definitions.Keys).ToArray())
        {
            _indexedDefinitions.Remove(name);
            _pendingConstraintIndexes.Remove(name);
            _closingCoverageStarts.Remove(name);
            _abandonedConstraintIndexes.Remove(name);
        }
        foreach (var (name, definition) in definitions)
        {
            _indexedDefinitions[name] = definition;
            if (definition is ClosesStreamConstraintDefinition)
            {
                _closingCoverageStarts.TryAdd(name, coverageStart ?? State.SequenceNumber);
            }
        }
    }

    void MarkConstraintIndexPending(ConstraintName name, EventSequenceNumber from)
    {
        if (_closingCoverageStarts.TryGetValue(name, out var start) && from < start) from = start;
        if (!_pendingConstraintIndexes.TryGetValue(name, out var previous) || from < previous)
        {
            _pendingConstraintIndexes[name] = from;
        }
    }

    void MarkConstraintIndexGap(EventSequenceNumber from)
    {
        foreach (var name in _indexedDefinitions.Keys)
        {
            MarkConstraintIndexPending(name, from);
        }
        UpdateConstraintIndexWatermark();
    }

    void UpdateConstraintIndexWatermark()
    {
        State.ConstraintIndexWatermark = _pendingConstraintIndexes.Values.Aggregate(
            _constraintIndexPosition < State.SequenceNumber ? _constraintIndexPosition : State.SequenceNumber,
            (position, pending) => pending < position ? pending : position);
        _metrics?.ConstraintIndexLag(State.SequenceNumber.Value - State.ConstraintIndexWatermark.Value);
    }

    bool IsGovernedByPendingIndex(EventTypeId type) => _pendingConstraintIndexes.Keys.Any(name =>
        _indexedDefinitions.TryGetValue(name, out var definition) && definition switch
        {
            UniqueConstraintDefinition unique => unique.SupportsEventType(type),
            ClosesStreamConstraintDefinition => true,
            _ => false
        });

    async Task PrepareConstraintIndexes(IEnumerable<EventTypeId> types)
    {
        SynchronizeIndexedDefinitions();
        if (_constraintIndexPosition < State.SequenceNumber) MarkConstraintIndexGap(_constraintIndexPosition);
        if (types.Any(IsGovernedByPendingIndex)) await RepairConstraintIndexesBeforeAppend();
    }
}
