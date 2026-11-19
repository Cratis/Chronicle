// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_an_index_unavailable_refusal : Specification
{
    IAppendResult[] _results;

    void Because()
    {
        _results =
        [
            Serializer.DeepClone(new Contracts.Sequences.AppendResponse
            {
                Errors = ["ConstraintIndexUnavailable"], IsRetryable = true, RetryAfter = TimeSpan.FromSeconds(1)
            }).ToClient(),
            Serializer.DeepClone(new Contracts.Sequences.AppendManyResponse
            {
                Errors = ["ConstraintIndexUnavailable"], IsRetryable = true, RetryAfter = TimeSpan.FromSeconds(1)
            }).ToClient(),
            Serializer.DeepClone(new Contracts.Sequences.AppendOnceResponse
            {
                Errors = ["ConstraintIndexUnavailable"], IsRetryable = true, RetryAfter = TimeSpan.FromSeconds(1)
            }).ToClient(),
            Serializer.DeepClone(new Contracts.Sequences.AppendManyOnceResponse
            {
                Errors = ["ConstraintIndexUnavailable"], IsRetryable = true, RetryAfter = TimeSpan.FromSeconds(1)
            }).ToClient()
        ];
    }

    [Fact] void should_preserve_retryability_for_every_append_shape() => _results.All(result => result.IsRetryable).ShouldBeTrue();
    [Fact] void should_preserve_the_retry_delay_for_every_append_shape() => _results.All(result => result.RetryAfter == TimeSpan.FromSeconds(1)).ShouldBeTrue();
    [Fact] void should_preserve_the_existing_error() => _results.All(result => result.Errors.Single() == "ConstraintIndexUnavailable").ShouldBeTrue();
    [Fact] void should_not_report_success() => _results.Any(result => result.IsSuccess).ShouldBeFalse();
    [Fact] void should_not_report_a_business_violation() => _results.Any(result => result.HasConstraintViolations).ShouldBeFalse();
}
