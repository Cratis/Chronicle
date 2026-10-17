// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_the_run_is_stopped_during_a_checkpoint_flush : given.a_performing_job_step
{
    readonly TaskCompletionSource<IReadOnlyDictionary<SiloAddress, Guid[]>> _flush = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Catch<JobStepResult> _result;
    int _writesBeforeTheStop;
    int _writesAfterTheFlushCompleted;

    TestStorageStats _stateStorageStats => _silo.StorageManager.GetStorageStats(nameof(JobStepState))!;

    void Establish()
    {
        // The first flush is the baseline taken before any event is dispatched; the second is the one the stop interrupts.
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
            Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] }),
            _flush.Task);
        _stateStorageStats.ResetCounts();
    }

    async Task Because()
    {
        var performing = _jobStep.InvokePerformStep(_performState);
        await WaitForTheFlush();
        _writesBeforeTheStop = _stateStorageStats.Writes;

        _jobStep.Run!.Close();
        _flush.SetResult(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] });
        _result = await performing;
        _writesAfterTheFlushCompleted = _stateStorageStats.Writes;
    }

    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_write_state_before_the_stop() => _writesBeforeTheStop.ShouldEqual(0);
    [Fact] void should_not_commit_the_flush_after_the_stop() => _writesAfterTheFlushCompleted.ShouldEqual(0);
    [Fact] void should_keep_the_last_durable_checkpoint() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_count_the_batch_the_next_run_handles_again() =>
        _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(0);

    async Task WaitForTheFlush()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (_observerServiceClient.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserverServiceClient.FlushAllFor)) < 2)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The step never started flushing the checkpoint");
            }

            await Task.Delay(10);
        }
    }
}
