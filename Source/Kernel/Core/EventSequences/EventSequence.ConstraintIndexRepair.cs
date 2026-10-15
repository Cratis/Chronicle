// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    void PrepareConstraintIndexRepair()
    {
        SynchronizeIndexedDefinitions();
        if (_constraintIndexPosition < State.SequenceNumber) MarkConstraintIndexGap(_constraintIndexPosition);
    }

    async Task RepairConstraintIndexesBeforeAppend()
    {
        PrepareConstraintIndexRepair();
        var now = ConstraintIndexTimeProvider.GetUtcNow();
        if (now < _nextConstraintRepairAttempt || (_lastAppendRepairEnded is { } ended && now - ended < TimeSpan.FromSeconds(5))) return;
        try
        {
            await RunConstraintIndexRepairTurn(TimeSpan.FromSeconds(5), false, CancellationToken.None);
        }
        finally
        {
            _lastAppendRepairEnded = ConstraintIndexTimeProvider.GetUtcNow();
        }
    }

    async Task RunConstraintIndexRepairTurn(TimeSpan duration, bool timerTurn, CancellationToken cancellationToken)
    {
        var end = ConstraintIndexTimeProvider.GetUtcNow() + duration;
        using var soft = new CancellationTokenSource(duration - TimeSpan.FromMilliseconds(250), ConstraintIndexTimeProvider);
        using var hard = new CancellationTokenSource(duration, ConstraintIndexTimeProvider);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(soft.Token, cancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(hard.Token, cancellationToken);
        await ContinueConstraintIndexRepair(timerTurn, end, budget.Token, deadline.Token, hard.Token);
    }

    async Task ContinueConstraintIndexRepair(bool timerTurn, DateTimeOffset end, CancellationToken budget, CancellationToken deadline, CancellationToken snapshotLimit)
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
            var poisoned = _constraintIndexPoisonTracker.Observe(result.Failed, _constraintIndexReplay.StorageCallSucceeded);
            foreach (var (name, failure) in poisoned)
            {
                _constraintIndexReplay.SettlePoison(name, failure.Position!);
                logger.ConstraintIndexPoisonEvent(_eventSequenceKey, name, failure.Position!, failure.Error);
                _metrics?.ConstraintIndexPoisonEvent();
            }
            CountReplaySettledErrors(result.SettledWithError + poisoned.Count);
            if (_constraintIndexReplay.RangeReadFailed)
            {
                MarkConstraintIndexGap(State.ConstraintIndexWatermark!);
            }
            if (result.Failed.Keys.Any(name => !poisoned.ContainsKey(name)) || result.Abandoned.Any())
            {
                BackOffConstraintRepair();
            }
            else if (poisoned.Count > 0)
            {
                _constraintRepairFailures = 0;
                _nextConstraintRepairAttempt = default;
            }
            if (!result.IsCompleted) return;
            CompleteConstraintIndexRepair(timerTurn);
        }
        catch (Exception error)
        {
            // Uncooperative reads are discarded; ordinary budget stops retain the staged fold.
            if (_constraintIndexReplay?.WritingConstraint is { } name) _abandonedConstraintIndexes.Add(name);
            _constraintIndexReplay = null;
            BackOffConstraintRepair();
            logger.ConstraintIndexRepairFailed(_eventSequenceKey, State.ConstraintIndexWatermark!, State.SequenceNumber, error);
            return;
        }

        _metrics?.ConstraintIndexRepaired();
        try
        {
            var remaining = end - ConstraintIndexTimeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero) throw new TimeoutException("No repair budget remains for the state snapshot.");
            using var snapshot = new CancellationTokenSource(remaining < TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250), ConstraintIndexTimeProvider);

            // Timer disposal cancels the fold token, not the completed repair's snapshot.
            using var snapshotDeadline = CancellationTokenSource.CreateLinkedTokenSource(snapshot.Token, snapshotLimit);
            await WriteStateAsync().WaitAsync(snapshotDeadline.Token);
        }
        catch (Exception error)
        {
            logger.ConstraintIndexRepairSnapshotNotPersisted(_eventSequenceKey, State.ConstraintIndexWatermark!, error);
        }
        finally
        {
            UpdateConstraintIndexRepairTimer();
        }
    }

    void CompleteConstraintIndexRepair(bool timerTurn)
    {
        foreach (var scope in _replayScopes)
        {
            var name = scope.Definition.Name;
            if (_replayAbandonedIndexes.Contains(name)) continue;
            if (_abandonedConstraintIndexes.Remove(name))
            {
                _constraintIndexesAwaitingConfirmation.Add(name);
                continue;
            }
            if (_constraintIndexesAwaitingConfirmation.Contains(name))
            {
                if (!timerTurn) continue;
                _constraintIndexesAwaitingConfirmation.Remove(name);
            }
            if (_pendingConstraintIndexes.TryGetValue(name, out var from) && from < scope.From) continue;
            if (_replayEndPoint < State.SequenceNumber) _pendingConstraintIndexes[name] = _replayEndPoint;
            else _pendingConstraintIndexes.Remove(name);
            _constraintIndexPoisonTracker.ForgetSettledBefore(name, _replayEndPoint);
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
        PrepareConstraintIndexRepair();
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
            ConstraintIndexTimeProvider = ConstraintIndexTimeProvider,
            StorageErrorClassifier = _storageErrorClassifier,
            SettledPoison = _constraintIndexPoisonTracker.Settled
        };
    }
}
