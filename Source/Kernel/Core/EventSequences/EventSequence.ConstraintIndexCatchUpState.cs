// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    Task? _catchUpSnapshotWrite;

    async Task PersistCatchUpState(CancellationToken token)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(250), ConstraintIndexTimeProvider);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, token);
        if (_catchUpSnapshotWrite is { } previous)
        {
            try
            {
                await previous.WaitAsync(deadline.Token);
            }
            finally
            {
                if (previous.IsCompleted) _catchUpSnapshotWrite = null;
            }
        }
        deadline.Token.ThrowIfCancellationRequested();
        var write = _catchUpSnapshotWrite = WriteStateAsync();
        try
        {
            await write.WaitAsync(deadline.Token);
        }
        finally
        {
            // Never let an abandoned clear land after a subsequent refusal barrier. Later
            // turns must observe this attempt's completion before issuing another snapshot.
            if (write.IsCompleted) _catchUpSnapshotWrite = null;
        }
    }

    async Task PersistCatchUpRefusal(IEnumerable<ConstraintName> names, CancellationToken token)
    {
        State.ConstraintIndexCatchUpRefusing = (State.ConstraintIndexCatchUpRefusing ?? []).Union(names).ToArray();
        await PersistCatchUpState(token);
    }

    async Task ClearCatchUpRefusal(CancellationToken token)
    {
        var previous = State.ConstraintIndexCatchUpRefusing;
        State.ConstraintIndexCatchUpRefusing = null;
        try
        {
            await PersistCatchUpState(token);
        }
        catch
        {
            State.ConstraintIndexCatchUpRefusing = previous;
            throw;
        }
    }

    async Task CompleteConstraintCatchUp(CancellationToken token)
    {
        var boundary = State.ConstraintIndexCatchUpBelow;
        State.ConstraintIndexCatchUpBelow = null;
        try
        {
            await ClearCatchUpRefusal(token);
        }
        catch
        {
            State.ConstraintIndexCatchUpBelow = boundary;
            throw;
        }
        foreach (var name in _indexedDefinitions.Keys) _catchUpPoisonTracker.ForgetSettledBefore(name, boundary!);
        _metrics?.ConstraintIndexRepaired();
        _catchUpReplay = null;
        _catchUpTracking = false;
        _catchUpExcludedThrough = null;
        _catchUpExcluded.Clear();
        _constraintRepairFailures = 0;
        _nextConstraintRepairAttempt = default;
        ConstraintIndexCatchUpSlots.For(storage).Complete(this);
    }
}
