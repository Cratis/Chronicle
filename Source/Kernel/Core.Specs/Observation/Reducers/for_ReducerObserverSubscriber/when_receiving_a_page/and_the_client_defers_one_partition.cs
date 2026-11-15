// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

public class and_the_client_defers_one_partition : given.a_page_subscriber
{
    void Establish() => _clientResults["first"] = new(new(ObserverSubscriberState.Deferred, 1UL, [], string.Empty), null);

    Task Because() => Deliver();

    [Fact] void should_defer_that_partition() => ResultFor("first").State.ShouldEqual(ObserverSubscriberState.Deferred);
    [Fact] void should_report_nothing_of_that_partition_as_handled() => ResultFor("first").LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_settle_the_other_partition_as_handled() => ResultFor("second").State.ShouldEqual(ObserverSubscriberState.Ok);
}
