// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.for_Observer.given;

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_awaiting;

public class and_invalidated_while_pending : given.a_continuation_checkpoint
{
    readonly held_storage_operation<int> _operation = new();
    readonly held_storage_operation<int> _genericOperation = new();
    bool _stillJoined;
    bool _genericStillJoined;

    async Task Because()
    {
        var waiting = ContinueAfter(_operation.Invoke);
        var genericWaiting = ContinueAfterResult(_genericOperation.Invoke);
        try
        {
            await _operation.WaitForStart();
            await _genericOperation.WaitForStart();
            _lifetime.Invalidate();
            _stillJoined = !waiting.IsCompleted;
            _genericStillJoined = !genericWaiting.IsCompleted;
        }
        finally
        {
            _operation.Complete(42);
            _genericOperation.Complete(43);
        }
        _error = await Catch.Exception(() => waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _genericError = await Catch.Exception(() => genericWaiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    [Fact] void should_remain_joined_to_the_issued_operation() => _stillJoined.ShouldBeTrue();
    [Fact] void should_remain_joined_to_the_issued_result() => _genericStillJoined.ShouldBeTrue();
    [Fact] void should_let_the_issued_operation_finish() => _operation.Finished.IsCompleted.ShouldBeTrue();
    [Fact] void should_let_the_issued_result_finish() => _genericOperation.Finished.IsCompleted.ShouldBeTrue();
    [Fact] void should_reject_the_completion() => _error.ShouldBeOfExactType<ObserverContinuationInvalidated>();
    [Fact] void should_reject_the_result() => _genericError.ShouldBeOfExactType<ObserverContinuationInvalidated>();
    [Fact] void should_not_continue() => _continued.ShouldBeFalse();
    [Fact] void should_not_continue_with_a_result() => _genericContinued.ShouldBeFalse();
}
