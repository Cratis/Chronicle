// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// Changes arriving while a read is running leave one pending signal behind, answered by one read once the running
/// one is done - however many changes there were.
/// </summary>
public class and_changes_arrive_during_a_read : given.a_shared_change_stream
{
    const int Changes = 5;

    readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

    given.a_recording_observer<int> _observer;
    int _readsWhenTheFollowUpArrived;

    async Task Because()
    {
        _observer = new();
        using var subscription = Observe(readNumber =>
        {
            if (readNumber != 1)
            {
                return Task.FromResult(readNumber);
            }

            _readStarted.TrySetResult();
            return _releaseRead.Task.ContinueWith(_ => readNumber, TaskScheduler.Default);
        }).Subscribe(_observer);
        await _readStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The loop asks for the next batch only once it has signalled the previous one, so having been asked for one
        // more than the changes reported means every one of them has been signalled while the read was blocked.
        for (var change = 0; change < Changes; change++)
        {
            Cursor(0).Report();
        }

        await Cursor(0).WaitForMoves(Changes + 1);

        _releaseRead.TrySetResult();
        await _observer.WaitForValues(2);
        _readsWhenTheFollowUpArrived = Reads;
    }

    [Fact] void should_deliver_the_read_that_was_running() => _observer.Values[0].ShouldEqual(1);
    [Fact] void should_deliver_the_follow_up_read() => _observer.Values[1].ShouldEqual(2);
    [Fact] void should_read_only_once_more_for_the_whole_burst() => _readsWhenTheFollowUpArrived.ShouldEqual(2);
}
