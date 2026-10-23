// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.given;

namespace Cratis.Chronicle.Observation.for_DeliveryFences;

public class when_the_hold_has_expired : Specification
{
    readonly DeliveryFences _fences = new();
    readonly DeliveryClock _clock = new();

    void Because()
    {
        _fences.Hold(new("partition", _clock.GetUtcNow().AddSeconds(300), []));
        _clock.Advance(TimeSpan.FromSeconds(300));
    }

    [Fact] void should_not_hold_the_partition() => _fences.IsHeld("partition", _clock.GetUtcNow()).ShouldBeFalse();
    [Fact] void should_remove_expired_fences_from_the_snapshot() => _fences.Active(_clock.GetUtcNow()).ShouldBeEmpty();
}
