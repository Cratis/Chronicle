// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_deactivating;

public class with_in_flight_dispatch : given.a_queue_with_a_gated_worker
{
    bool _completedBeforeWorker;

    async Task Because()
    {
        var stop = _queue.OnDeactivateAsync(default, CancellationToken.None);
        _completedBeforeWorker = stop.IsCompleted;
        _worker.SetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_join_the_queue_worker() => _completedBeforeWorker.ShouldBeFalse();
}
