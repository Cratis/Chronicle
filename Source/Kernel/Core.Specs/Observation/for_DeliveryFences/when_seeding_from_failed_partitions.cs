// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;

namespace Cratis.Chronicle.Observation.for_DeliveryFences;

public class when_seeding_from_failed_partitions : Specification
{
    readonly DeliveryFences _fences = new();
    readonly DeliveryClock _clock = new();

    void Because()
    {
        var failures = new FailedPartitions();
        var timeout = failures.RegisterAttempt("timeout", 42, [], string.Empty, FailureKind.Timeout);
        timeout.LastAttempt.Occurred = _clock.GetUtcNow();
        var expired = failures.RegisterAttempt("expired", 42, [], string.Empty, FailureKind.Timeout);
        expired.LastAttempt.Occurred = _clock.GetUtcNow().AddSeconds(-301);
        failures.RegisterAttempt("handling", 42, [], string.Empty, FailureKind.Handling);
        _fences.SeedFrom(failures, TimeSpan.FromSeconds(300));
    }

    [Fact] void should_hold_a_legacy_timeout_for_the_full_hold() => _fences.Active(_clock.GetUtcNow()).Single().ReleaseBy.ShouldEqual(_clock.GetUtcNow().AddSeconds(300));
    [Fact] void should_not_invent_a_current_target_for_a_legacy_attempt() => _fences.Active(_clock.GetUtcNow()).Single().Targets.ShouldBeEmpty();
    [Fact] void should_not_hold_an_expired_timeout() => _fences.IsHeld("expired", _clock.GetUtcNow()).ShouldBeFalse();
    [Fact] void should_not_hold_a_handling_failure() => _fences.IsHeld("handling", _clock.GetUtcNow()).ShouldBeFalse();
}
