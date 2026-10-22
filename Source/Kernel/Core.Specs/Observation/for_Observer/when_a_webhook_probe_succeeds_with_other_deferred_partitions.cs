// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_succeeds_with_other_deferred_partitions : given.a_webhook_ready_to_probe
{
    FailedPartition _retryAfter;
    FailedPartition _circuitOpen;
    PartitionDeferral _retryAfterDeferral;
    PartitionDeferral _circuitOpenDeferral;

    void Establish()
    {
        _retryAfter = _failedPartitionsState.RegisterAttempt("retry-after", 50UL, ["429"], "", FailureKind.Handling);
        _retryAfter.IsQuarantined = true;
        _retryAfter.WebhookDisposition = WebhookFailureDisposition.Retryable;
        _retryAfterDeferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, _now, _now + TimeSpan.FromHours(3), 50UL);
        _retryAfter.Deferral = _retryAfterDeferral;
        _circuitOpen = _failedPartitionsState.RegisterAttempt("circuit-open", 60UL, ["circuit open"], "", FailureKind.Handling);
        _circuitOpen.IsQuarantined = true;
        _circuitOpen.WebhookDisposition = WebhookFailureDisposition.Retryable;
        _circuitOpenDeferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, _now, _now + TimeSpan.FromHours(4), 60UL);
        _circuitOpen.Deferral = _circuitOpenDeferral;
    }

    Task Because() => Tick();

    [Fact] void should_probe_only_the_due_partition() => _webhook.Received(1).OnNext((Key)"partition", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_keep_the_other_retry_after_deadline_and_cursor() => _retryAfter.Deferral.ShouldEqual(_retryAfterDeferral);
    [Fact] void should_keep_the_other_circuit_deadline_and_cursor() => _circuitOpen.Deferral.ShouldEqual(_circuitOpenDeferral);
    [Fact] void should_reset_the_retry_after_budget() => _retryAfter.AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_reset_the_circuit_open_budget() => _circuitOpen.AttemptsInCurrentBudget.ShouldEqual(0);
}
