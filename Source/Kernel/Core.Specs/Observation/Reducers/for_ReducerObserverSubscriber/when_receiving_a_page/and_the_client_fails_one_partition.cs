// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

/// <summary>
/// The pipeline writes no state for a failed partition, so even a successful prefix of it is not reported as handled.
/// </summary>
public class and_the_client_fails_one_partition : given.a_page_subscriber
{
    void Establish() => _clientResults["second"] = new(ObserverSubscriberResult.Failed(2UL, "boom"), null);

    Task Because() => Deliver();

    [Fact] void should_fail_that_partition() => ResultFor("second").State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_report_nothing_of_that_partition_as_handled() => ResultFor("second").LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_settle_the_other_partition_as_handled() => ResultFor("first").State.ShouldEqual(ObserverSubscriberState.Ok);
}
