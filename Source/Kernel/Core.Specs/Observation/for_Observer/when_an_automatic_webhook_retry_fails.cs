// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_automatic_webhook_retry_fails : given.a_webhook_observer
{
    protected override Configuration.Observers CreateWebhookRetryConfig() => new Configuration.Webhooks().Retry;

    PartitionDeferral _expired;
    FailedPartition _failure;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _expired = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now.AddMinutes(-2), now.AddMinutes(-1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _expired);
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling, _failure.Id, _expired.Id);

    [Fact] void should_consume_one_actual_attempt() => _failure.Attempts.Count().ShouldEqual(1);
    [Fact] void should_replace_the_expired_deadline() => _failure.Deferral!.Id.ShouldNotEqual(_expired.Id);
    [Fact] void should_use_backoff_instead_of_the_old_reason() => _failure.Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RetryBackoff);
    [Fact] void should_persist_a_future_deadline() => (_failure.Deferral!.Until > DateTimeOffset.UtcNow).ShouldBeTrue();
    [Fact] void should_bound_the_first_attempt_jitter() => (_failure.Deferral!.Until - _failure.Deferral.Since).TotalSeconds.ShouldBeInRange(0.8d, 1.2d);
    [Fact] void should_preserve_the_unhandled_sequence() => _failure.Deferral!.ResumeFrom.ShouldEqual(new Concepts.Events.EventSequenceNumber(42));
}
