// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_handler_throws : given.a_reducer_dispatcher
{
    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
        .ThrowsAsync(new Exception("Reducer exploded"));

    async Task Because() => await _dispatcher.Dispatch(_handler, OperationWith(3, 4), "ns", Publish);

    [Fact] void should_publish_one_result() => _published.Count.ShouldEqual(1);
    [Fact] void should_fail() => _published[0].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_carry_the_message() => _published[0].ExceptionMessages.ShouldContain("Reducer exploded");
    [Fact] void should_not_advance_the_checkpoint() => _published[0].LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
}
