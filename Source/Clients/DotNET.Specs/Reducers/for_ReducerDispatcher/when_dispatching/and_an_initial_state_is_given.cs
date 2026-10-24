// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_an_initial_state_is_given : given.a_reducer_dispatcher
{
    object? _initial;

    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
    {
        _initial = call.Arg<object?>();
        return new ReduceResult(new State("after"), 7, [], string.Empty);
    });

    async Task Because()
    {
        var operation = OperationWith(6, 7);
        operation.InitialState = "{\"name\":\"before\"}";
        await _dispatcher.Dispatch(_handler, operation, "ns", Publish);
    }

    [Fact] void should_pass_the_deserialized_initial_state() => _initial.ShouldEqual(new State("before"));
    [Fact] void should_publish_one_result() => _published.Count.ShouldEqual(1);
    [Fact] void should_publish_the_serialized_read_model() => _published[0].ReadModelState!.ShouldContain("after");
    [Fact] void should_succeed() => _published[0].State.ShouldEqual(ObservationState.Success);
    [Fact] void should_report_the_last_successful_event() => _published[0].LastSuccessfulObservation.ShouldEqual(7UL);
    [Fact] void should_echo_the_partition() => _published[0].Partition.ShouldEqual("partition");
    [Fact] void should_echo_the_delivery_id() => _published[0].DeliveryId.ShouldEqual(42UL);
    [Fact] void should_run_as_the_system_identity() => _identityProvider.Received().SetCurrentIdentity(Identity.System);
}
