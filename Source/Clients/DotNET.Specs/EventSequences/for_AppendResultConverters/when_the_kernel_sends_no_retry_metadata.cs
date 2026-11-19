// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_the_kernel_sends_no_retry_metadata : Specification
{
    IAppendResult[] _results;

    void Because()
    {
        _results =
        [
            Serializer.DeepClone(new Contracts.Sequences.AppendResponse { Errors = ["ConstraintIndexUnavailable"] }).ToClient(),
            Serializer.DeepClone(new Contracts.Sequences.AppendManyResponse { Errors = ["ConstraintIndexUnavailable"] }).ToClient(),
            Serializer.DeepClone(new Contracts.Sequences.AppendOnceResponse { Errors = ["ConstraintIndexUnavailable"] }).ToClient(),
            Serializer.DeepClone(new Contracts.Sequences.AppendManyOnceResponse { Errors = ["ConstraintIndexUnavailable"] }).ToClient()
        ];
    }

    [Fact] void should_not_infer_retryability_from_the_error() => _results.Any(result => result.IsRetryable).ShouldBeFalse();
    [Fact] void should_not_invent_a_retry_delay() => _results.Any(result => result.RetryAfter is not null).ShouldBeFalse();
    [Fact] void should_preserve_the_failure() => _results.Any(result => result.IsSuccess).ShouldBeFalse();
}
