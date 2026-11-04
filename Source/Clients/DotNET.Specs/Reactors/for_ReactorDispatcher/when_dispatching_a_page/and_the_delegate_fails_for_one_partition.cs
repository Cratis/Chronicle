// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page;

public class and_the_delegate_fails_for_one_partition : given.a_reactor_dispatcher
{
    async Task Because() => await _dispatcher.Dispatch(
        _handler,
        (@event, _) => @event.Context.SequenceNumber == 2UL ? throw new Exception("Bridge unavailable") : Task.CompletedTask,
        PageWith(("first", [1, 2]), ("second", [3, 4])),
        "ns",
        Publish,
        CancellationToken.None);

    PartitionResult First => _published[0].Partitions.Single(_ => _.Partition == "first");
    PartitionResult Second => _published[0].Partitions.Single(_ => _.Partition == "second");

    [Fact] void should_publish_one_result_for_the_page() => _published.Count.ShouldEqual(1);
    [Fact] void should_answer_with_one_result_per_partition() => _published[0].Partitions.Count.ShouldEqual(2);
    [Fact] void should_echo_the_delivery_id() => _published[0].DeliveryId.ShouldEqual(42UL);
    [Fact] void should_fail_the_failing_partition() => First.State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_keep_what_the_failing_partition_handled() => First.LastSuccessfulObservation.ShouldEqual(1UL);
    [Fact] void should_carry_the_message_on_the_failing_partition() => First.ExceptionMessages.ShouldContain("Bridge unavailable");
    [Fact] void should_let_the_other_partition_succeed() => Second.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_advance_the_other_partition_to_its_last_event() => Second.LastSuccessfulObservation.ShouldEqual(4UL);
}
