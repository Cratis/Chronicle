// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.for_Observer.given;

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_awaiting;

public class and_the_operation_faults : given.a_continuation_checkpoint
{
    readonly held_storage_operation<int> _operation = new();
    readonly InvalidOperationException _failure = new("Storage failed");

    async Task Because()
    {
        var waiting = ContinueAfter(_operation.Invoke);
        var genericWaiting = ContinueAfterResult(_operation.Invoke);
        try
        {
            await _operation.WaitForStart();
            _lifetime.Invalidate();
        }
        finally
        {
            _operation.Fail(_failure);
        }
        _error = await Catch.Exception(() => waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _genericError = await Catch.Exception(() => genericWaiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    [Fact] void should_preserve_the_real_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_preserve_the_real_result_failure() => _genericError.ShouldEqual(_failure);
    [Fact] void should_not_continue() => _continued.ShouldBeFalse();
    [Fact] void should_not_continue_with_a_result() => _genericContinued.ShouldBeFalse();
}
