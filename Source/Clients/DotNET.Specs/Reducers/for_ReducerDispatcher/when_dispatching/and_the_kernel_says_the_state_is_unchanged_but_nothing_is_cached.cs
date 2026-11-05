// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_kernel_says_the_state_is_unchanged_but_nothing_is_cached : given.a_reducer_dispatcher
{
    async Task Because()
    {
        var operation = OperationWith(6, 7);
        operation.InitialStateUnchanged = true;
        await _dispatcher.Dispatch(_handler, operation, "ns", Publish);
    }

    [Fact] void should_fail_so_the_kernel_redelivers_the_full_state() => _published[0].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_explain_why() => _published[0].ExceptionMessages.Single().ShouldContain("no cached state");
    [Fact] void should_not_call_the_handler() => _handler.DidNotReceiveWithAnyArgs().OnNext(default!, default, default!);
    [Fact] void should_not_publish_a_read_model() => _published[0].ReadModelState.ShouldBeNull();
}
