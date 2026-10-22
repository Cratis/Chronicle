// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently;

public class and_the_delivering_caller_has_ambient_state : given.a_delegate_with_gated_batches
{
    static readonly AsyncLocal<string?> _ambient = new();
    readonly TaskCompletionSource<string?> _seenByHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string? _seen;

    async Task Because()
    {
        // Stands in for whatever code the stream's reader runs on, such as a long-gone HTTP request.
        _ambient.Value = "caller";
        try
        {
            Deliver("order-1", 1, 101);
        }
        finally
        {
            _ambient.Value = null;
        }

        Release("order-1", 1);
        await Answered(101);
        _seen = await _seenByHandler.Task.WaitAsync(_deadline);
    }

    protected override void OnHandled(string name) => _seenByHandler.TrySetResult(_ambient.Value);

    [Fact] void should_not_run_the_handler_in_the_callers_execution_context() => _seen.ShouldBeNull();
}
