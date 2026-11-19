// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences.for_AppendResult;

public class when_the_failure_is_not_solely_an_index_refusal : Specification
{
    AppendResult[] _singles;
    AppendManyResult[] _batches;
    AppendOnceResult[] _keyedSingles;
    AppendManyOnceResult[] _keyedBatches;

    void Because()
    {
        ConstraintViolation violation = new("event", EventSequenceNumber.Unavailable, ConstraintType.Unique, "unique", "duplicate", new());
        ConcurrencyViolation concurrency = new(EventSourceId.Unspecified, EventSequenceNumber.First, EventSequenceNumber.Unavailable);
        _singles =
        [
            AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First),
            AppendResult.Failed(CorrelationId.New(), [AppendError.AppendOutcomeUnknown]),
            AppendResult.Failed(CorrelationId.New(), [(AppendError)"other failure"]),
            AppendResult.Failed(CorrelationId.New(), [AppendError.ConstraintIndexUnavailable, (AppendError)"other failure"]),
            AppendResult.Failed(CorrelationId.New(), [violation]),
            AppendResult.Failed(CorrelationId.New(), concurrency),
            new() { Errors = [AppendError.ConstraintIndexUnavailable], ConstraintViolations = [violation] },
            new() { Errors = [AppendError.ConstraintIndexUnavailable], ConcurrencyViolation = concurrency }
        ];
        _batches =
        [
            AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]),
            new() { Errors = [AppendError.AppendOutcomeUnknown] },
            new() { Errors = [(AppendError)"other failure"] },
            new() { Errors = [AppendError.ConstraintIndexUnavailable, (AppendError)"other failure"] },
            AppendManyResult.Failed(CorrelationId.New(), [violation]),
            AppendManyResult.Failed(CorrelationId.New(), [concurrency]),
            new() { Errors = [AppendError.ConstraintIndexUnavailable], ConstraintViolations = [violation] },
            new() { Errors = [AppendError.ConstraintIndexUnavailable], ConcurrencyViolations = [concurrency] }
        ];
        _keyedSingles = _singles.Select(result => AppendOnceResult.From(new(result))).ToArray();
        _keyedBatches = _batches.Select(result => AppendManyOnceResult.From(new(result))).ToArray();
    }

    [Fact] void should_not_make_single_attempts_retryable() => _singles.Any(result => result.IsRetryable).ShouldBeFalse();
    [Fact] void should_not_suggest_single_retry_delays() => _singles.Any(result => result.RetryAfter is not null).ShouldBeFalse();
    [Fact] void should_not_make_batch_attempts_retryable() => _batches.Any(result => result.IsRetryable).ShouldBeFalse();
    [Fact] void should_not_suggest_batch_retry_delays() => _batches.Any(result => result.RetryAfter is not null).ShouldBeFalse();
    [Fact] void should_not_make_keyed_single_attempts_retryable() => _keyedSingles.Any(result => result.IsRetryable).ShouldBeFalse();
    [Fact] void should_not_suggest_keyed_single_retry_delays() => _keyedSingles.Any(result => result.RetryAfter is not null).ShouldBeFalse();
    [Fact] void should_not_make_keyed_batch_attempts_retryable() => _keyedBatches.Any(result => result.IsRetryable).ShouldBeFalse();
    [Fact] void should_not_suggest_keyed_batch_retry_delays() => _keyedBatches.Any(result => result.RetryAfter is not null).ShouldBeFalse();
}
