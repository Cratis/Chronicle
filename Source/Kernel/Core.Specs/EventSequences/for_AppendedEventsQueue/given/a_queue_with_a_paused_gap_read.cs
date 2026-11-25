// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class a_queue_with_a_paused_gap_read : a_queue_verifying_live_gaps
{
    protected readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _resumeRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _reads;

    protected static AppendedEvent Doorbell()
    {
        var @event = MatchingAt(105);
        return @event with { Context = @event.Context with { Tags = [new("included"), new("replacement")] } };
    }

    protected override async Task<IEventCursor> ReadHistory(EventSequenceNumber from, EventSequenceNumber until)
    {
        var cursor = HistoryCursor(from, until);
        if (Interlocked.Increment(ref _reads) == 1)
        {
            _readStarted.TrySetResult();
            await _resumeRead.Task;
        }

        return cursor;
    }

    void Destroy() => _resumeRead.TrySetResult();
}
