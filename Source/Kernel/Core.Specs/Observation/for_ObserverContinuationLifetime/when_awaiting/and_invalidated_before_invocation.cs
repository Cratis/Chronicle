// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_awaiting;

public class and_invalidated_before_invocation : given.a_continuation_checkpoint
{
    async Task Because()
    {
        _lifetime.Invalidate();
        _error = await Catch.Exception(() => ContinueAfter(() =>
        {
            _invocations++;
            return Task.CompletedTask;
        }));
        _genericError = await Catch.Exception(() => ContinueAfterResult(() =>
        {
            _invocations++;
            return Task.FromResult(42);
        }));
    }

    [Fact] void should_not_invoke_either_factory() => _invocations.ShouldEqual(0);
    [Fact] void should_report_invalidation() => _error.ShouldBeOfExactType<ObserverContinuationInvalidated>();
    [Fact] void should_report_invalidation_for_a_result() => _genericError.ShouldBeOfExactType<ObserverContinuationInvalidated>();
    [Fact] void should_not_continue() => _continued.ShouldBeFalse();
    [Fact] void should_not_continue_with_a_result() => _genericContinued.ShouldBeFalse();
}
