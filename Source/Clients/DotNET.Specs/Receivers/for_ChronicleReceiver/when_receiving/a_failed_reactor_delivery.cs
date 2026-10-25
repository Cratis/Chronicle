// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class a_failed_reactor_delivery : given.a_receiver
{
    void Establish() => _reactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns(new ReactorResult
    {
        Partition = "order-42", State = ObservationState.Failed, LastSuccessfulObservation = 11, ExceptionMessages = ["handler failed"], ExceptionStackTrace = "handler stack"
    });
    Task Because() => Receive();
    [Fact] void should_report_failure() => _outcome.Status.ShouldEqual(DeliveryStatus.Failed);
    [Fact] void should_keep_partial_progress() => _outcome.LastSuccessfulObservation.Value.ShouldEqual(11UL);
    [Fact] void should_keep_messages() => _outcome.ExceptionMessages.Single().ShouldEqual("handler failed");
    [Fact] void should_keep_the_stack_trace() => _outcome.ExceptionStackTrace.ShouldEqual("handler stack");
}
