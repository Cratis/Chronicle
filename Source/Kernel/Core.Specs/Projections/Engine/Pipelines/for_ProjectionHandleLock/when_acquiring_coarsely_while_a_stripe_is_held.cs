// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionHandleLock;

public class when_acquiring_coarsely_while_a_stripe_is_held : Specification
{
    ProjectionHandleLock _lock;
    EventSourceId _eventSourceId;
    bool _coarseAcquiredWhileStripeHeld;
    bool _coarseAcquiredAfterStripeReleased;

    void Establish()
    {
        _lock = new ProjectionHandleLock();
        _eventSourceId = "a-partition";
    }

    async Task Because()
    {
        var stripe = await _lock.AcquireFor(_eventSourceId);
        var coarse = _lock.AcquireCoarse();

        _coarseAcquiredWhileStripeHeld = await CompletesWithin(coarse, TimeSpan.FromMilliseconds(200));

        stripe.Dispose();

        _coarseAcquiredAfterStripeReleased = await CompletesWithin(coarse, TimeSpan.FromSeconds(5));
        if (_coarseAcquiredAfterStripeReleased)
        {
            (await coarse).Dispose();
        }
    }

    [Fact]
    void should_block_the_coarse_acquisition_while_the_stripe_is_held() => _coarseAcquiredWhileStripeHeld.ShouldBeFalse();

    [Fact]
    void should_grant_the_coarse_acquisition_once_the_stripe_is_released() => _coarseAcquiredAfterStripeReleased.ShouldBeTrue();

    static async Task<bool> CompletesWithin(Task task, TimeSpan timeout) => await Task.WhenAny(task, Task.Delay(timeout)) == task;
}
