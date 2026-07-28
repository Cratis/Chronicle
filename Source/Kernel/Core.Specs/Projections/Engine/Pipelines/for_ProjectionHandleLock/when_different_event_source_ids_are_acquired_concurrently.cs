// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionHandleLock;

public class when_different_event_source_ids_are_acquired_concurrently : Specification
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    ProjectionHandleLock _lock;
    EventSourceId _first;
    EventSourceId _second;
    bool _bothHeldConcurrently;

    void Establish()
    {
        _lock = new ProjectionHandleLock();

        // Pick two event source ids that map to different stripes so they can be held at the same time; if they
        // shared a stripe they would (correctly) serialize and the concurrency this spec proves would not occur.
        _first = "partition-a";
        _second = Enumerable.Range(0, 1000)
            .Select(index => (EventSourceId)$"partition-b-{index}")
            .First(candidate => ProjectionHandleLock.StripeIndexFor(candidate) != ProjectionHandleLock.StripeIndexFor(_first));
    }

    async Task Because()
    {
        var firstAcquired = new TaskCompletionSource();
        var secondAcquired = new TaskCompletionSource();

        async Task HoldFirst()
        {
            using var handle = await _lock.AcquireFor(_first);
            firstAcquired.SetResult();
            _bothHeldConcurrently = await Completes(secondAcquired.Task);
        }

        async Task HoldSecond()
        {
            using var handle = await _lock.AcquireFor(_second);
            secondAcquired.SetResult();
            await Completes(firstAcquired.Task);
        }

        await Task.WhenAll(HoldFirst(), HoldSecond());
    }

    [Fact]
    void should_hold_both_event_source_ids_at_the_same_time() => _bothHeldConcurrently.ShouldBeTrue();

    static async Task<bool> Completes(Task task) => await Task.WhenAny(task, Task.Delay(_timeout)) == task;
}
