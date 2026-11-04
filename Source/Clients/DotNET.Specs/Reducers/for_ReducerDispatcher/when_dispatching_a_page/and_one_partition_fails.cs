// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching_a_page;

public class and_one_partition_fails : given.a_reducer_dispatcher
{
    void Establish()
    {
        _handler.OnNext(Arg.Is<IEnumerable<AppendedEvent>>(_ => _.First().Context.SequenceNumber == 1UL), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
            .ThrowsAsync(new Exception("Reducer exploded"));
        _handler.OnNext(Arg.Is<IEnumerable<AppendedEvent>>(_ => _.First().Context.SequenceNumber == 3UL), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
            .Returns(new ReduceResult(new State("reduced"), 4UL, [], string.Empty));
    }

    async Task Because() => await _dispatcher.Dispatch(_handler, PageWith(("first", null, [1, 2]), ("second", "{\"name\":\"before\"}", [3, 4])), "ns", Publish);

    PartitionResult First => _published[0].Partitions.Single(_ => _.Partition == "first");
    PartitionResult Second => _published[0].Partitions.Single(_ => _.Partition == "second");

    [Fact] void should_publish_one_result_for_the_page() => _published.Count.ShouldEqual(1);
    [Fact] void should_echo_the_delivery_id() => _published[0].DeliveryId.ShouldEqual(42UL);
    [Fact] void should_fail_the_failing_partition() => First.State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_carry_the_message_on_the_failing_partition() => First.ExceptionMessages.ShouldContain("Reducer exploded");
    [Fact] void should_reduce_the_other_partition() => Second.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_return_the_reduced_state_of_the_other_partition() => Second.ReadModelState.ShouldContain("reduced");
    [Fact] void should_advance_the_other_partition_to_its_last_event() => Second.LastSuccessfulObservation.ShouldEqual(4UL);
}
