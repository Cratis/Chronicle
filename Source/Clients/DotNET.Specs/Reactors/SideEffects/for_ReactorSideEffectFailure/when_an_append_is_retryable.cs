// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure;

public class when_an_append_is_retryable : Specification
{
    ReactorSideEffectFailure _failure;

    void Because()
    {
        var result = AppendResult.Failed(CorrelationId.New(), [new AppendError("ConstraintIndexUnavailable")]) with
        {
            IsRetryable = true,
            RetryAfter = TimeSpan.FromSeconds(1)
        };
        _failure = ReactorSideEffectFailure.FromAppendResult(result, []);
    }

    [Fact] void should_preserve_retryability_in_the_exception() => _failure.AppendFailures.Single().IsRetryable.ShouldBeTrue();
    [Fact] void should_preserve_the_delay_in_the_exception() => _failure.AppendFailures.Single().RetryAfter.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_preserve_the_error_in_the_exception() => _failure.AppendFailures.Single().Errors.ShouldContainOnly("ConstraintIndexUnavailable");
}
