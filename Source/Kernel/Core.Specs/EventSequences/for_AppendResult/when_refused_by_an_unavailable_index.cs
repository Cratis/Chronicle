// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences.for_AppendResult;

public class when_refused_by_an_unavailable_index : Specification
{
    AppendResult _single;
    AppendManyResult _batch;
    AppendOnceResult _keyedSingle;
    AppendManyOnceResult _keyedBatch;

    void Because()
    {
        _single = AppendResult.Failed(CorrelationId.New(), [AppendError.ConstraintIndexUnavailable]);
        _batch = new() { CorrelationId = CorrelationId.New(), Errors = [AppendError.ConstraintIndexUnavailable] };
        _keyedSingle = AppendOnceResult.From(new(_single));
        _keyedBatch = AppendManyOnceResult.From(new(_batch));
    }

    [Fact] void should_make_the_single_refusal_retryable() => _single.IsRetryable.ShouldBeTrue();
    [Fact] void should_suggest_a_single_retry_delay() => _single.RetryAfter.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_make_the_batch_refusal_retryable() => _batch.IsRetryable.ShouldBeTrue();
    [Fact] void should_suggest_a_batch_retry_delay() => _batch.RetryAfter.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_make_the_keyed_single_refusal_retryable() => _keyedSingle.IsRetryable.ShouldBeTrue();
    [Fact] void should_suggest_a_keyed_single_retry_delay() => _keyedSingle.RetryAfter.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_make_the_keyed_batch_refusal_retryable() => _keyedBatch.IsRetryable.ShouldBeTrue();
    [Fact] void should_suggest_a_keyed_batch_retry_delay() => _keyedBatch.RetryAfter.ShouldEqual(TimeSpan.FromSeconds(1));
}
