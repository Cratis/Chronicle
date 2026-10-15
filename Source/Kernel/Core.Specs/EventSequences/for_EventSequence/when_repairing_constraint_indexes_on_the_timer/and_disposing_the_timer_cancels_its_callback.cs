// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_disposing_the_timer_cancels_its_callback : given.an_event_sequence_with_repairable_indexes
{
    readonly CancellationTokenSource _callback = new();
    readonly TaskCompletionSource _snapshotStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _finishSnapshot = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IGrainTimer _timer;
    bool _disposedBeforeSnapshot;
    bool _waitedForSnapshot;
    protected override EventSequenceNumber InitialNext => 1;

    async Task Establish()
    {
        await Seed(0);
        _timer = Substitute.For<IGrainTimer>();
        _timer.When(timer => timer.Dispose()).Do(_ => _callback.Cancel());
        var field = typeof(EventSequence).GetField("_constraintIndexRepairTimer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((IGrainTimer)field.GetValue(_eventSequence)!).Dispose();
        field.SetValue(_eventSequence, _timer);
        _snapshots.BeforeWrite = () =>
        {
            _disposedBeforeSnapshot = _callback.IsCancellationRequested;
            _snapshotStarted.TrySetResult();
            return _finishSnapshot.Task;
        };
    }

    async Task Because()
    {
        var turn = (Task)typeof(EventSequence).GetMethod("RepairConstraintIndexesOnTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(_eventSequence, [_callback.Token])!;
        await _snapshotStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _waitedForSnapshot = !turn.IsCompleted;

        // Even external callback cancellation must not cancel a completed repair's state write.
        await _callback.CancelAsync();
        _finishSnapshot.TrySetResult();
        await turn.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    void Destroy() => _callback.Dispose();

    [Fact] void should_not_dispose_the_timer_before_the_snapshot() => _disposedBeforeSnapshot.ShouldBeFalse();
    [Fact] void should_await_the_snapshot_before_completing_the_turn() => _waitedForSnapshot.ShouldBeTrue();
    [Fact] void should_dispose_the_idle_timer_after_persistence() => _timer.Received(1).Dispose();
    [Fact] async Task should_persist_the_completed_watermark() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
}
