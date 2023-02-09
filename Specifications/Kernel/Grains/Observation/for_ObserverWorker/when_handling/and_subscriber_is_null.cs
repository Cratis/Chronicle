// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker.when_handling;

public class and_subscriber_is_null : given.an_observer_worker
{
    AppendedEvent @event;
    EventSequenceNumber expected_sequence_number;

    void Establish()
    {
        state.RunningState = ObserverRunningState.Active;
        worker.SetSubscriberType(null!);
        @event = AppendedEvent.EmptyWithEventType(EventType.Unknown);
        expected_sequence_number = (ulong)Random.Shared.Next();
        state.NextEventSequenceNumber = expected_sequence_number;
    }

    Task Because() => worker.Handle(@event);

    [Fact] void should_not_call_the_subscriber() => subscriber.VerifyNoOtherCalls();
    [Fact] void should_not_move_the_sequence_number() => state.NextEventSequenceNumber.ShouldEqual(expected_sequence_number);
}
