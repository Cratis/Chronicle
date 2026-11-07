// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_a_previous_reduce_failed : given.a_reducer_dispatcher
{
    int _calls;

    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(_ =>
        ++_calls == 1
            ? new ReduceResult(new State("first"), 7, [], string.Empty)
            : new ReduceResult(null, 7, ["boom"], "stack"));

    async Task Because()
    {
        await _dispatcher.Dispatch(_handler, OperationWith(6), "ns", Publish);
        await _dispatcher.Dispatch(_handler, OperationWith(7), "ns", Publish);

        var unchanged = OperationWith(8);
        unchanged.InitialStateUnchanged = true;
        await _dispatcher.Dispatch(_handler, unchanged, "ns", Publish);
    }

    [Fact] void should_have_failed_the_second_reduce() => _published[1].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_trust_an_unchanged_marker_afterwards() => _published[2].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_have_called_the_handler_for_the_marker() => _calls.ShouldEqual(2);
}
