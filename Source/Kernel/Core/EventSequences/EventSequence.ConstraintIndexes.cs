// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    readonly Dictionary<ConstraintName, IConstraintDefinition> _indexedDefinitions = [];
    readonly Dictionary<ConstraintName, EventSequenceNumber> _pendingConstraintIndexes = [];
    readonly Dictionary<ConstraintName, EventSequenceNumber> _closingCoverageStarts = [];
    readonly HashSet<ConstraintName> _abandonedConstraintIndexes = [];
    readonly HashSet<ConstraintName> _replayAbandonedIndexes = [];
    readonly HashSet<ConstraintName> _constraintIndexesAwaitingConfirmation = [];
    readonly IStorageErrorClassifier _storageErrorClassifier = storageErrorClassifier;
    readonly ConstraintIndexPoisonTracker _constraintIndexPoisonTracker = new(storageErrorClassifier);
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

    async Task InitializeConstraintIndexes()
    {
        _indexedDefinitions.Clear();
        _pendingConstraintIndexes.Clear();
        _closingCoverageStarts.Clear();
        _abandonedConstraintIndexes.Clear();
        _constraintIndexesAwaitingConfirmation.Clear();
        _constraintIndexReplay = null;
        _lastAppendRepairEnded = null;
        _nextConstraintRepairAttempt = default;
        _constraintRepairFailures = 0;

        var upgrading = State.ConstraintIndexWatermark is null;
        State.ConstraintIndexWatermark = State.ConstraintIndexWatermark is { } watermark && watermark < State.SequenceNumber ? watermark : State.SequenceNumber;
        _constraintIndexPosition = State.SequenceNumber;
        SynchronizeIndexedDefinitions(State.ConstraintIndexWatermark);
        if (upgrading && State.SequenceNumber > EventSequenceNumber.First && _indexedDefinitions.Values.OfType<UniqueConstraintDefinition>().Any())
        {
            State.ConstraintIndexCatchUpBelow ??= State.SequenceNumber;
        }
        var staleMarker = State.ConstraintIndexCatchUpBelow is null && State.ConstraintIndexCatchUpRefusing is not null;
        if (staleMarker) State.ConstraintIndexCatchUpRefusing = null;
        _catchUpExcludedThrough = State.ConstraintIndexCatchUpBelow;
        if (upgrading || staleMarker) await WriteStateAsync();
        UpdateConstraintIndexRepairTimer();
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
            foreach (var name in definitions.Where(pair => _indexedDefinitions.TryGetValue(pair.Key, out var previous) && !pair.Value.Equals(previous)).Select(pair => pair.Key))
            {
                _constraintIndexPoisonTracker.ForgetSettledBefore(name, State.SequenceNumber);
                _catchUpPoisonTracker.ForgetSettledBefore(name, State.SequenceNumber);
            }
            _constraintIndexReplay = null;
            _catchUpReplay = null;
            _catchUpExcluded.Clear();
            _catchUpExcludedThrough = State.ConstraintIndexCatchUpBelow;
        }
        foreach (var name in _indexedDefinitions.Keys.Concat(_pendingConstraintIndexes.Keys).Except(definitions.Keys).ToArray())
        {
            _constraintIndexPoisonTracker.ForgetSettledBefore(name, State.SequenceNumber);
            _catchUpPoisonTracker.ForgetSettledBefore(name, State.SequenceNumber);
            _indexedDefinitions.Remove(name);
            _pendingConstraintIndexes.Remove(name);
            _closingCoverageStarts.Remove(name);
            _abandonedConstraintIndexes.Remove(name);
            _constraintIndexesAwaitingConfirmation.Remove(name);
        }
        foreach (var (name, definition) in definitions)
        {
            _indexedDefinitions[name] = definition;
            if (definition is ClosesStreamConstraintDefinition)
            {
                _closingCoverageStarts.TryAdd(name, coverageStart ?? State.SequenceNumber);
            }
        }
        UpdateConstraintIndexRepairTimer();
    }

    void MarkConstraintIndexPending(ConstraintName name, EventSequenceNumber from)
    {
        if (_closingCoverageStarts.TryGetValue(name, out var start) && from < start) from = start;
        if (!_pendingConstraintIndexes.TryGetValue(name, out var previous) || from < previous)
        {
            _pendingConstraintIndexes[name] = from;
        }
        UpdateConstraintIndexRepairTimer();
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
        UpdateConstraintIndexRepairTimer();
    }

    bool IsGovernedByPendingIndex(EventTypeId type) => _pendingConstraintIndexes.Keys
        .Concat(State.ConstraintIndexCatchUpRefusing ?? [])
        .Concat(_constraintIndexPosition < State.SequenceNumber ? _indexedDefinitions.Keys : [])
        .Any(name =>
        _indexedDefinitions.TryGetValue(name, out var definition) && definition switch
        {
            UniqueConstraintDefinition unique => unique.SupportsEventType(type),
            ClosesStreamConstraintDefinition => true,
            _ => false
        });

    async Task PrepareConstraintIndexes(IEnumerable<EventTypeId> types)
    {
        SynchronizeIndexedDefinitions();
        if ((_pendingConstraintIndexes.Count > 0 || _constraintIndexPosition < State.SequenceNumber) && types.Any(IsGovernedByPendingIndex)) await RepairConstraintIndexesBeforeAppend();
    }
}
