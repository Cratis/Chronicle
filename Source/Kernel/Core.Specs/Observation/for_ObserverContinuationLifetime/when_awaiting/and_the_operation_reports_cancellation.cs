// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_awaiting;

public class and_the_operation_reports_cancellation : given.a_continuation_checkpoint
{
    readonly OperationCanceledException _failure = new("Storage canceled");

    async Task Because()
    {
        _error = await Catch.Exception(() => ContinueAfter(() =>
        {
            _lifetime.Invalidate();
            return Task.FromException(_failure);
        }));
    }

    [Fact] void should_preserve_the_operations_own_cancellation() => _error.ShouldEqual(_failure);
    [Fact] void should_not_continue() => _continued.ShouldBeFalse();
}
