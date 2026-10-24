// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently;

public class and_the_delivering_request_has_completed_before_the_handler_runs : given.a_delegate_with_an_http_request
{
    Seen _seen;

    async Task Because()
    {
        BeginRequest();
        Deliver("order-1", 1, 101);

        // The request is over: aborted and its services disposed, but the stale context is still attached to the caller.
        await _requestAborted.CancelAsync();
        await _requestServices.DisposeAsync();

        Release("order-1", 1);
        await Answered(101);
        _seen = await _seenByHandler.Task.WaitAsync(_deadline);
        EndRequest();
    }

    [Fact] void should_still_run_the_handler() => _results.Count.ShouldEqual(1);
    [Fact] void should_not_expose_the_http_context_to_the_handler() => _seen.HasHttpContext.ShouldBeFalse();
    [Fact] void should_not_expose_request_scoped_services_to_the_handler() => _seen.HasRequestMarker.ShouldBeFalse();
    [Fact] void should_not_cancel_the_handler() => _seen.HandlerCancelled.ShouldBeFalse();
}
