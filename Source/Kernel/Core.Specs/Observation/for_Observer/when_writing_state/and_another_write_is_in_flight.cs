// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_another_write_is_in_flight : given.an_observer_with_interceptable_state
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _firstWrite;
    int _writesBeforeRelease;

    async Task Establish()
    {
        _interceptedState.OnWrite = async () =>
        {
            _started.TrySetResult();
            await _release.Task;
        };
        _firstWrite = _observer.SetHandledStats(42UL);
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    async Task Because()
    {
        var secondWrite = _observer.SetHandledStats(43UL);
        _writesBeforeRelease = _interceptedState.Writes.Count;
        _release.SetResult();
        await Task.WhenAll(_firstWrite, secondWrite).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_serialize_the_storage_writes() => _writesBeforeRelease.ShouldEqual(1);
    [Fact] void should_write_both_snapshots() => _interceptedState.Writes.Count.ShouldEqual(2);
    [Fact] void should_write_the_latest_progress_last() => _interceptedState.Writes.Last().LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
}
