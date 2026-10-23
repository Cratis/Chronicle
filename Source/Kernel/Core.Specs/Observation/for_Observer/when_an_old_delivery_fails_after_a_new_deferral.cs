// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_old_delivery_fails_after_a_new_deferral : given.a_webhook_observer
{
    FailedPartition _failure;
    PartitionDeferral _current;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _current = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now.AddMinutes(1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _current);
    }

    Task Because() => _observer.PartitionFailed("partition", new(42), ["old failure"], string.Empty, FailureKind.Handling, _failure.Id, Guid.NewGuid());

    [Fact] void should_not_consume_an_attempt() => _failure.Attempts.ShouldBeEmpty();
    [Fact] void should_keep_the_new_deadline() => _failure.Deferral.ShouldEqual(_current);
}
