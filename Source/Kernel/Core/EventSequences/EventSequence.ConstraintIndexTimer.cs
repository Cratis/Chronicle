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

    void UpdateConstraintIndexRepairTimer()
    {
        var hasGap = _indexedDefinitions.Count > 0 && _constraintIndexPosition < State.SequenceNumber;
        if (_pendingConstraintIndexes.Count == 0 && !hasGap && State.ConstraintIndexCatchUpBelow is null && _rebuildingConstraintIndexes.Count == 0)
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

    async Task RepairConstraintIndexesOnTimer(CancellationToken cancellationToken)
    {
        _constraintIndexRepairTimerRunning = true;
        try
        {
            PrepareConstraintIndexRepair();
            var now = ConstraintIndexTimeProvider.GetUtcNow();
            var repairDue = _pendingConstraintIndexes.Count > 0 && now >= _nextConstraintRepairAttempt;
            var catchUpDue = ConstraintCatchUpDue(now);
            if (repairDue && (!catchUpDue || !_catchUpTurnNext))
            {
                _catchUpTurnNext = true;
                await RunConstraintIndexRepairTurn(TimeSpan.FromSeconds(2), true, cancellationToken);
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
