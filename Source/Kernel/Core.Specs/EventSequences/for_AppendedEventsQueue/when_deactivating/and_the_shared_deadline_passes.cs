// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_deactivating;

public class and_the_shared_deadline_passes : given.a_queue_with_a_gated_worker
{
    Task _stop;

    async Task Because()
    {
        _stop = _queue.OnDeactivateAsync(default, CancellationToken.None);
        await _deadline.CancelAsync();
        await _stop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_return_at_the_shared_deadline() => _stop.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_claim_the_dispatch_completed() => _worker.Task.IsCompleted.ShouldBeFalse();
}
