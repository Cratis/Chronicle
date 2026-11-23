// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_awaiting;

public class and_the_factory_throws : given.a_continuation_checkpoint
{
    readonly InvalidOperationException _failure = new("Factory failed");

    async Task Because()
    {
        _error = await Catch.Exception(() => ContinueAfter(() =>
        {
            _invocations++;
            throw _failure;
        }));
        _genericError = await Catch.Exception(() => ContinueAfterResult(() =>
        {
            _invocations++;
            throw _failure;
        }));
    }

    [Fact] void should_invoke_each_factory_once() => _invocations.ShouldEqual(2);
    [Fact] void should_preserve_the_factory_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_preserve_the_result_factory_failure() => _genericError.ShouldEqual(_failure);
    [Fact] void should_not_continue() => _continued.ShouldBeFalse();
    [Fact] void should_not_continue_with_a_result() => _genericContinued.ShouldBeFalse();
}
