// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task RepairConstraintIndexesBeforeAppend()
    {
        var now = ConstraintIndexTimeProvider.GetUtcNow();
        if (now < _nextConstraintRepairAttempt || (_lastAppendRepairEnded is { } ended && now - ended < TimeSpan.FromSeconds(5))) return;
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(5), ConstraintIndexTimeProvider);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(5250), ConstraintIndexTimeProvider);
        try
        {
            await ContinueConstraintIndexRepair(budget.Token, deadline.Token);
        }
        finally
        {
            _lastAppendRepairEnded = ConstraintIndexTimeProvider.GetUtcNow();
        }
    }

    async Task ContinueConstraintIndexRepair(CancellationToken budget, CancellationToken deadline)
    {
        try
        {
            _constraintIndexReplay ??= CreateConstraintIndexReplay();
            var result = await _constraintIndexReplay.Continue(int.MaxValue, budget).WaitAsync(deadline);
            foreach (var name in result.Abandoned)
            {
                _abandonedConstraintIndexes.Add(name);
                _replayAbandonedIndexes.Add(name);
            }
            CountReplaySettledErrors(result.SettledWithError);
            foreach (var (name, failure) in result.Failed)
            {
                logger.ConstraintIndexUpdateOutcome(_eventSequenceKey, name, failure.Position ?? State.ConstraintIndexWatermark!, State.ConstraintIndexWatermark!, State.SequenceNumber, ConstraintIndexUpdateOutcome.Failed, failure.Error);
            }
            if (_constraintIndexReplay.RangeReadFailed)
            {
                MarkConstraintIndexGap(State.ConstraintIndexWatermark!);
                _constraintIndexReplay = null;
            }
            if (result.Failed.Count > 0 || result.Abandoned.Any()) BackOffConstraintRepair();
            if (!result.IsCompleted) return;
            CompleteConstraintIndexRepair();
        }
        catch (Exception error)
        {
            // A hard timeout discards the uncooperative fold, not unrelated healthy indexes.
            // Range-read failures are reported by the replay and widen refusal above.
            _constraintIndexReplay = null;
            BackOffConstraintRepair();
            logger.ConstraintIndexRepairFailed(_eventSequenceKey, State.ConstraintIndexWatermark!, State.SequenceNumber, error);
            return;
        }

        try
        {
            await WriteStateAsync().WaitAsync(deadline);
            _metrics?.ConstraintIndexRepaired();
        }
        catch (Exception error)
        {
            // Index writes already completed. A lagging snapshot must not make healthy indexes pending.
            logger.ConstraintIndexRepairFailed(_eventSequenceKey, State.ConstraintIndexWatermark!, State.SequenceNumber, error);
        }
    }

    void CompleteConstraintIndexRepair()
    {
        foreach (var scope in _replayScopes)
        {
            var name = scope.Definition.Name;
            if (_replayAbandonedIndexes.Contains(name)) continue;

            // This full later fold is the append-turn equivalent of the timer's confirmation turn.
            _abandonedConstraintIndexes.Remove(name);
            if (_pendingConstraintIndexes.TryGetValue(name, out var from) && from < scope.From) continue;
            if (_replayEndPoint < State.SequenceNumber) _pendingConstraintIndexes[name] = _replayEndPoint;
            else _pendingConstraintIndexes.Remove(name);
        }
        if (_constraintIndexPosition < _replayEndPoint) _constraintIndexPosition = _replayEndPoint;
        _constraintIndexReplay = null;
        _constraintRepairFailures = 0;
        _nextConstraintRepairAttempt = default;
        UpdateConstraintIndexWatermark();
    }

    void BackOffConstraintRepair()
    {
        _constraintRepairFailures++;
        _nextConstraintRepairAttempt = ConstraintIndexTimeProvider.GetUtcNow().AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(6, _constraintRepairFailures - 1))));
    }

    void CountReplaySettledErrors(int count)
    {
        for (; _replaySettledErrors < count; _replaySettledErrors++) _metrics?.ConstraintIndexSettledWithError();
    }

    ConstraintIndexReplay CreateConstraintIndexReplay()
    {
        _replayScopes = _pendingConstraintIndexes.Where(pair => _indexedDefinitions.ContainsKey(pair.Key)).Select(pair => new ConstraintIndexReplayScope(_indexedDefinitions[pair.Key], pair.Value)).ToArray();
        _replayAbandonedIndexes.Clear();
        _replaySettledErrors = 0;
        _replayEndPoint = State.SequenceNumber;
        var namespaceStorage = storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace);
        return new(
            EventSequenceStorage,
            namespaceStorage.GetUniqueConstraintsStorage(_eventSequenceId),
            ClosedStreamsStorage,
            new StoredConstraintEventContent(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, EventTypesStorage, jsonComplianceManagerProvider, expandoObjectConverter),
            constraintIndexReplayLogger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConstraintIndexReplay>.Instance,
            _replayScopes,
            _replayEndPoint)
        {
            ConstraintIndexTimeProvider = ConstraintIndexTimeProvider
        };
    }
}
