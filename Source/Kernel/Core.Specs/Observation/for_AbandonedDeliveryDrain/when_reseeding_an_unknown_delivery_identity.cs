// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_reseeding_an_unknown_delivery_identity : given.a_captured_delivery
{
    bool _held;

    async Task Because()
    {
        var failures = new FailedPartitions();
        var failure = failures.RegisterAttempt("partition", 42, [], string.Empty, FailureKind.Timeout);
        failure.LastAttempt.Occurred = _clock.GetUtcNow();
        failure.LastAttempt.DeliveryFence = new(_clock.GetUtcNow().AddSeconds(1), [new("unknown", "invalid", "A", "invalid")]);
        var fences = new DeliveryFences();
        fences.SeedFrom(failures, TimeSpan.FromSeconds(300));
        var fence = fences.Active(_clock.GetUtcNow()).Single();
        var drain = AbandonedDeliveryDrain.Until(fence, _factory, _clock, NullLogger.Instance, CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(299));
        _held = !drain.IsCompleted;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await drain;
    }

    [Fact] void should_hold_for_the_full_reconstructed_hold() => _held.ShouldBeTrue();
    [Fact] void should_not_drain_current_subscription_targets() => _subscriber.DidNotReceive().Drain();
}
