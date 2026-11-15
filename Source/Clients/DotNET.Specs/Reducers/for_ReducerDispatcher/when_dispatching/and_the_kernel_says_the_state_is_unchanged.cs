// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_kernel_says_the_state_is_unchanged : given.a_reducer_dispatcher
{
    readonly List<object?> _initialStates = [];

    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
    {
        _initialStates.Add(call.Arg<object?>());
        return new ReduceResult(new State("after-" + _initialStates.Count), 7, [], string.Empty);
    });

    async Task Because()
    {
        await _dispatcher.Dispatch(_handler, OperationWith(6, 7), "ns", Publish);

        var unchanged = OperationWith(8);
        unchanged.InitialStateUnchanged = true;
        await _dispatcher.Dispatch(_handler, unchanged, "ns", Publish);
    }

    [Fact] void should_reduce_from_the_state_returned_last_time() => _initialStates[1].ShouldEqual(new State("after-1"));
    [Fact] void should_succeed() => _published[1].State.ShouldEqual(ObservationState.Success);
    [Fact] void should_return_the_new_state() => _published[1].ReadModelState.ShouldContain("after-2");
}
