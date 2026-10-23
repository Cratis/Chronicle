// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.given;

namespace Cratis.Chronicle.Observation.for_DeliveryFences;

public class when_releasing_an_older_hold : Specification
{
    readonly DeliveryFences _fences = new();
    readonly DeliveryClock _clock = new();

    void Because()
    {
        var now = _clock.GetUtcNow();
        _fences.Hold(new("partition", now.AddSeconds(100), []));
        _fences.Hold(new("partition", now.AddSeconds(300), []));
        _fences.Release("partition", now.AddSeconds(100));
    }

    [Fact] void should_keep_the_newer_fence() => _fences.IsHeld("partition", _clock.GetUtcNow()).ShouldBeTrue();
    [Fact] void should_not_hold_other_partitions() => _fences.IsHeld("other", _clock.GetUtcNow()).ShouldBeFalse();
}
