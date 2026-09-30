// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_observing;

/// <summary>
/// The shared change stream stays open for the subscribers that remain when one of several leaves.
/// </summary>
/// <remarks>
/// The leaving subscriber leaves by its read failing, because a failing read ends the observation only after it has
/// left the stream: seeing the error is the signal that the leave has happened, where disposing a subscription
/// cancels it in the background with nothing to await.
/// </remarks>
public class and_one_of_two_subscribers_leaves : given.a_shared_change_stream
{
    given.a_recording_observer<int> _leaving;
    given.a_recording_observer<int> _remaining;
    bool _cursorWasDisposed;
    bool _streamWasStopped;

    async Task Because()
    {
        var leavingReads = 0;
        _leaving = new();
        _remaining = new();
        using var leaving = Observe(readNumber => ++leavingReads == 1
            ? Task.FromResult(readNumber)
            : Task.FromException<int>(new InvalidOperationException("The subscriber is leaving"))).Subscribe(_leaving);
        await _leaving.WaitForValues(1);
        using var remaining = Observe().Subscribe(_remaining);
        await _remaining.WaitForValues(1);

        Cursor(0).Report();
        await _leaving.WaitForError();
        await _remaining.WaitForValues(2);

        Cursor(0).Report();
        await _remaining.WaitForValues(3);
        _cursorWasDisposed = Cursor(0).Disposed.IsCompleted;
        _streamWasStopped = WatchToken(0).IsCancellationRequested;
    }

    [Fact] void should_share_one_change_stream() => Watches.ShouldEqual(1);
    [Fact] void should_not_dispose_the_cursor() => _cursorWasDisposed.ShouldBeFalse();
    [Fact] void should_not_stop_watching() => _streamWasStopped.ShouldBeFalse();
    [Fact] void should_keep_reading_for_the_remaining_subscriber() => _remaining.Values.Count.ShouldEqual(3);
}
