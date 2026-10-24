// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_the_queue_is_disposed_while_retrying : given.an_unreachable_observer
{
    readonly TaskCompletionSource _firstAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _observer.CatchUp().Returns(_ =>
    {
        _firstAttempt.TrySetResult();
        return Task.FromException(new InvalidOperationException("catch-up unreachable"));
    });

    async Task Because()
    {
        await DispatchFirstEvent();
        await _firstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _clock.WaitForNextDelay();
        _queue.Dispose();
        await _clock.AdvanceNextDelay();
    }

    [Fact] void should_abandon_the_trigger() => _observer.Received(1).CatchUp();
}
