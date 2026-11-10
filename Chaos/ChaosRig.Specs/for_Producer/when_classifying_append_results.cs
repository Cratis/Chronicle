// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_Producer;

/// <summary>
/// Specifies definitive refusal and ambiguous outcome classification.
/// </summary>
public class when_classifying_append_results : Specification
{
    AppendResult _success = new();

    void Because() => _success = new() { SequenceNumber = 0 };

    [Fact] void should_acknowledge_success() => Producer.StatusFor(_success).ShouldEqual("acked");
    [Fact] void should_acknowledge_an_identical_retry() => Producer.StatusFor(_success with { Outcome = AppendOutcome.AlreadyAppended }).ShouldEqual("acked");
    [Fact] void should_keep_ambiguous_errors_unknown() => Producer.StatusFor(new() { Errors = [AppendError.AppendOutcomeUnknown] }).ShouldEqual("unknown");
    [Fact] void should_keep_other_errors_unknown() => Producer.StatusFor(new() { Errors = [AppendError.Unknown] }).ShouldEqual("unknown");
    [Fact] void should_record_concurrency_refusals_as_failed() => Producer.StatusFor(new() { ConcurrencyViolation = new("source", 0, 1) }).ShouldEqual("failed");
    [Fact] void should_record_constraint_refusals_as_failed() => Producer.StatusFor(new() { ConstraintViolations = [new("type", 0, ConstraintType.Unique, "unique", "refused", new())] }).ShouldEqual("failed");
    [Fact] void should_record_idempotency_refusals_as_failed() => Producer.StatusFor(new() { IdempotencyConflicts = [new(new("key"), 0, new("type", 1), 1, IdempotencyConflictReason.Content)] }).ShouldEqual("failed");
}
