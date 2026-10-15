// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    readonly ConstraintIndexPoisonTracker _catchUpPoisonTracker = new(storageErrorClassifier);
    int _catchUpSettledErrors;

    bool CatchUpTurnFailed(ConstraintIndexReplayResult result)
    {
        var replay = _catchUpReplay!;
        foreach (var (name, failure) in result.Failed)
        {
            logger.ConstraintIndexUpdateOutcome(_eventSequenceKey, name, failure.Position ?? State.ConstraintIndexCatchUpBelow!, State.ConstraintIndexWatermark!, State.SequenceNumber, ConstraintIndexUpdateOutcome.Failed, failure.Error);
        }

        // A corrupt input can be settled before it causes a write. Never poison-settle a
        // partially released group's failed claim: its refusal must last until it is written.
        var failures = replay.IsFolding ? result.Failed : new Dictionary<ConstraintName, ConstraintIndexReplayFailure>();
        var poisoned = _catchUpPoisonTracker.Observe(failures, replay.StorageCallSucceeded);
        foreach (var (name, failure) in poisoned)
        {
            replay.SettlePoison(name, failure.Position!);
            logger.ConstraintIndexPoisonEvent(_eventSequenceKey, name, failure.Position!, failure.Error);
            _metrics?.ConstraintIndexPoisonEvent();
        }
        for (; _catchUpSettledErrors < result.SettledWithError + poisoned.Count; _catchUpSettledErrors++) _metrics?.ConstraintIndexSettledWithError();
        foreach (var name in result.Abandoned)
        {
            logger.ConstraintIndexCatchUpAbandoned(_eventSequenceKey, name, State.ConstraintIndexCatchUpBelow!, State.ConstraintIndexWatermark!, State.SequenceNumber);
            _abandonedConstraintIndexes.Add(name);
            MarkConstraintIndexPending(name, State.ConstraintIndexCatchUpBelow!);
        }
        UpdateConstraintIndexWatermark();
        if (result.Failed.Keys.Any(name => !poisoned.ContainsKey(name)) || result.Abandoned.Any())
        {
            BackOffConstraintRepair();
            return true;
        }
        if (poisoned.Count > 0)
        {
            _constraintRepairFailures = 0;
            _nextConstraintRepairAttempt = default;
        }
        return false;
    }
}
