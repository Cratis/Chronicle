// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_catch_up_cannot_be_reached_at_first : given.an_unreachable_observer
{
    readonly TaskCompletionSource _catchupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _attempts;

    void Establish() => _observer.CatchUp().Returns(_ =>
    {
        if (Interlocked.Increment(ref _attempts) < 3)
        {
            return Task.FromException(new InvalidOperationException("catch-up unreachable"));
        }
        _catchupStarted.TrySetResult();
        return Task.CompletedTask;
    });

    async Task Because()
    {
        await DispatchFirstEvent();
        await _clock.AdvanceNextDelay();
        await _clock.AdvanceNextDelay();
        await _catchupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_retrying_until_catch_up_starts() => _observer.Received(3).CatchUp();
}
