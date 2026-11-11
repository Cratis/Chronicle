// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    IGrainTimer? _constraintIndexRepairTimer;
    bool _constraintIndexRepairTimerRunning;

    /// <summary>
    /// One bounded turn per tick keeps decision 0008 item 4's per-turn budget; repair and catch-up alternate when both are due.
    /// </summary>
    bool _catchUpTurnNext;

    bool HasConstraintIndexWork =>
        _pendingConstraintIndexes.Count > 0 ||
        HasConstraintIndexGap ||
        State.ConstraintIndexCatchUpBelow is not null ||
        _rebuildingConstraintIndexes.Count > 0;

    bool HasConstraintIndexGap => _indexedDefinitions.Count > 0 && _constraintIndexPosition < State.SequenceNumber;

    void UpdateConstraintIndexRepairTimer()
    {
        if (!HasConstraintIndexWork)
        {
            if (_constraintIndexRepairTimerRunning) return;
            _constraintIndexRepairTimer?.Dispose();
            _constraintIndexRepairTimer = null;
            return;
        }
        _constraintIndexRepairTimer ??= this.RegisterGrainTimer(
            RepairConstraintIndexesOnTimer,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(1),
                Period = TimeSpan.FromSeconds(1),
                Interleave = false,
                KeepAlive = true
            });
    }

    bool RepairTurnWins(bool repairDue, bool catchUpDue) => repairDue && (!catchUpDue || !_catchUpTurnNext);

    async Task RepairConstraintIndexesOnTimer(CancellationToken cancellationToken)
    {
        _constraintIndexRepairTimerRunning = true;
        try
        {
            await CheckHeldConstraintIndexRebuilds(cancellationToken);
            PrepareConstraintIndexRepair();
            var now = ConstraintIndexTimeProvider.GetUtcNow();
            var repairDue = _pendingConstraintIndexes.Count > 0 && now >= _nextConstraintRepairAttempt;
            var catchUpDue = ConstraintCatchUpDue(now);
            if (RepairTurnWins(repairDue, catchUpDue))
            {
                _catchUpTurnNext = true;
                await RunConstraintIndexRepairTurn(ConstraintIndexRepair.TimerTurnBudget, true, cancellationToken);
            }
            else if (catchUpDue)
            {
                _catchUpTurnNext = false;
                await RunConstraintIndexCatchUpTurn(cancellationToken);
            }
        }
        finally
        {
            _constraintIndexRepairTimerRunning = false;
            UpdateConstraintIndexRepairTimer();
        }
    }
}
