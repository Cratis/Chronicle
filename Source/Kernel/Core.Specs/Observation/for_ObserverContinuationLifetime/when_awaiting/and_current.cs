// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_awaiting;

public class and_current : given.a_continuation_checkpoint
{
    int _result;

    async Task Because()
    {
        await ContinueAfter(() =>
        {
            _invocations++;
            return Task.CompletedTask;
        });
        _result = await _checkpoint.Await(() =>
        {
            _invocations++;
            return Task.FromResult(42);
        });
    }

    [Fact] void should_invoke_both_factories() => _invocations.ShouldEqual(2);
    [Fact] void should_allow_the_continuation() => _continued.ShouldBeTrue();
    [Fact] void should_return_the_operation_result() => _result.ShouldEqual(42);
}
