// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

public class and_every_partition_is_handled : given.a_page_subscriber
{
    Task Because() => Deliver();

    [Fact] void should_send_the_whole_page_to_the_client_at_once() => _pagesSentToTheClient.Single().Select(_ => _.Partition.Value).ShouldContainOnly("first", "second");
    [Fact] void should_not_send_any_partition_on_its_own() => _mediator.DidNotReceive().OnNext(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReduceOperation>(), Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>());
    [Fact] void should_reduce_the_page_through_the_pipeline_once() => _firstPipeline.Received(1).ReducePage(Arg.Is<IReadOnlyList<ReducerContext>>(_ => _.Count == 2), Arg.Any<ReducerPageDelegate>(), Arg.Any<bool>());
    [Fact] void should_answer_for_every_partition() => _result.Results.Count.ShouldEqual(2);
    [Fact] void should_report_the_first_partition_as_handled() => ResultFor("first").LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_report_the_second_partition_as_handled() => ResultFor("second").LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)2UL);
}
