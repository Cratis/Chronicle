// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_delivery_is_rejected : given.a_traced_observer
{
    void Establish() => _failedPartitionsState.AddFailedPartition("partition", 42UL, ["Failure"], "Stack trace");

    Task Because() => _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);

    [Fact] void should_not_trace_the_delivery() => _activities.ShouldBeEmpty();
}
