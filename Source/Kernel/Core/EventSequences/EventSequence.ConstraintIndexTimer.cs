// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    IGrainTimer? _constraintIndexRepairTimer;
    bool _constraintIndexRepairTimerRunning;

    void UpdateConstraintIndexRepairTimer()
    {
        var hasGap = _indexedDefinitions.Count > 0 && _constraintIndexPosition < State.SequenceNumber;
        if (_pendingConstraintIndexes.Count == 0 && !hasGap)
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
            if (_pendingConstraintIndexes.Count == 0 || ConstraintIndexTimeProvider.GetUtcNow() < _nextConstraintRepairAttempt) return;
            await RunConstraintIndexRepairTurn(TimeSpan.FromSeconds(2), true, cancellationToken);
        }
        finally
        {
            _constraintIndexRepairTimerRunning = false;
            UpdateConstraintIndexRepairTimer();
        }
    }
}
