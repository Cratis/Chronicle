// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_the_original_silo_is_unavailable : given.a_captured_delivery
{
    bool _held;

    void Establish() => _subscriber.Drain().Returns(Task.FromException(new SiloUnavailableException("Original silo is unavailable")));

    async Task Because()
    {
        var drain = Drain();
        _clock.Advance(TimeSpan.FromSeconds(329));
        _held = !drain.IsCompleted;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await drain;
    }

    [Fact] void should_not_treat_a_dead_silo_as_drained() => _held.ShouldBeTrue();
    [Fact] void should_release_only_at_the_original_deadline() => _clock.GetUtcNow().ShouldEqual(_fence.ReleaseBy);
}
