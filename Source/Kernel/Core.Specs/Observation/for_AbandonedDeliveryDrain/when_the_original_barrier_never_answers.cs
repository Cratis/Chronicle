// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_the_original_barrier_never_answers : given.a_captured_delivery
{
    readonly TaskCompletionSource _barrier = new();
    bool _held;

    void Establish() => _subscriber.Drain().Returns(_barrier.Task);

    async Task Because()
    {
        var drain = Drain();
        _clock.Advance(TimeSpan.FromSeconds(329));
        _held = !drain.IsCompleted;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await drain;
    }

    [Fact] void should_hold_until_the_deadline() => _held.ShouldBeTrue();
    [Fact] void should_not_complete_the_remote_barrier() => _barrier.Task.IsCompleted.ShouldBeFalse();
}
