// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// A change stream that has given up stays registered until its subscribers have left, but is not joined: a new
/// subscriber gets a fresh stream. The old subscriber leaving afterwards must leave that fresh stream alone.
/// </summary>
public class and_a_subscriber_joins_after_the_stream_gave_up : given.a_shared_change_stream
{
    readonly TaskCompletionSource _firstReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseFirstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

    given.a_recording_observer<int> _stuck;
    given.a_recording_observer<int> _witness;
    given.a_recording_observer<int> _newcomer;
    bool _freshStreamWasDisposed;
    bool _freshStreamWasStopped;

    async Task Because()
    {
        // The first subscriber sits in its read while the stream fails, so it is still attached when the newcomer joins.
        _stuck = new();
        using var stuck = Observe(readNumber =>
        {
            if (readNumber != 1)
            {
                return Task.FromResult(readNumber);
            }

            _firstReadStarted.TrySetResult();
            return _releaseFirstRead.Task.ContinueWith(_ => readNumber, TaskScheduler.Default);
        }).Subscribe(_stuck);
        await _firstReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Learning of the failure is the only signal that the stream has recorded it: the witness is not reading,
        // so it is completed with the failure as soon as the stream gives up.
        _witness = new();
        using var witness = Observe().Subscribe(_witness);
        await _witness.WaitForValues(1);
        Cursor(0).Fail(new InvalidOperationException("The stream is broken"));
        await _witness.WaitForError();

        _newcomer = new();
        using var newcomer = Observe().Subscribe(_newcomer);
        await _newcomer.WaitForValues(1);

        _releaseFirstRead.TrySetResult();
        await _stuck.WaitForError();

        Cursor(1).Report();
        await _newcomer.WaitForValues(2);
        _freshStreamWasDisposed = Cursor(1).Disposed.IsCompleted;
        _freshStreamWasStopped = WatchToken(1).IsCancellationRequested;
    }

    [Fact] void should_open_a_new_change_stream_for_the_newcomer() => Watches.ShouldEqual(2);
    [Fact] void should_end_the_old_subscriber_with_the_failure() => _stuck.HasFailed.ShouldBeTrue();
    [Fact] void should_not_dispose_the_new_change_stream_when_the_old_subscriber_leaves() => _freshStreamWasDisposed.ShouldBeFalse();
    [Fact] void should_not_stop_the_new_change_stream_when_the_old_subscriber_leaves() => _freshStreamWasStopped.ShouldBeFalse();
    [Fact] void should_keep_serving_the_newcomer() => _newcomer.Values.Count.ShouldEqual(2);
    [Fact] void should_not_fail_the_newcomer() => _newcomer.HasFailed.ShouldBeFalse();
}
