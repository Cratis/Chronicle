// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_handler_reports_a_failure : given.a_reducer_dispatcher
{
    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
        .Returns(new ReduceResult(null, 3, ["boom"], "stack"));

    async Task Because() => await _dispatcher.Dispatch(_handler, OperationWith(3, 4), "ns", Publish);

    [Fact] void should_publish_one_result() => _published.Count.ShouldEqual(1);
    [Fact] void should_fail() => _published[0].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_carry_the_error_messages() => _published[0].ExceptionMessages.ShouldContainOnly("boom");
    [Fact] void should_carry_the_stack_trace() => _published[0].ExceptionStackTrace.ShouldEqual("stack");
    [Fact] void should_report_the_last_successful_event() => _published[0].LastSuccessfulObservation.ShouldEqual(3UL);
    [Fact] void should_not_publish_a_read_model() => _published[0].ReadModelState.ShouldBeNull();
}
