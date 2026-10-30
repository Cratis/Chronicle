// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_the_flush_has_not_completed : given.a_buffered_projection_job_step
{
    readonly TaskCompletionSource<IReadOnlyDictionary<SiloAddress, Guid[]>> _flush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _flushing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _flushes;
    bool _checkpointedWhileBuffered;
    int _countedWhileBuffered;

    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(_ =>
    {
        if (++_flushes != 2) return Task.FromResult(Windows(_window));
        _flushing.SetResult();
        return _flush.Task;
    });

    async Task Because()
    {
        var performing = _jobStep.InvokePerformStep(_performState);
        await _flushing.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _checkpointedWhileBuffered = _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue;
        _countedWhileBuffered = _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents));
        _flush.SetResult(Windows(_window));
        _result = await performing;
    }

    [Fact] void should_not_checkpoint_buffered_events() => _checkpointedWhileBuffered.ShouldBeFalse();
    [Fact] void should_not_count_buffered_events() => _countedWhileBuffered.ShouldEqual(0);
    [Fact] void should_checkpoint_after_the_flush() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_persist_each_collapsing_checkpoint() => _jobStep.IsCheckpointingAfterEveryBatch.ShouldBeTrue();
    [Fact] void should_succeed_after_the_flush() => _result.Match(result => !result.TryGetError(out _), _ => false).ShouldBeTrue();
}
