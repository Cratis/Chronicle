// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_a_client_subscriber_has_no_bulk_window : given.a_performing_job_step
{
    void Establish() => _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(Task.FromResult(ObserverSubscriberResult.Ok(first_event_sequence_number)));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_flush_projection_writes() => _observerServiceClient.DidNotReceive().FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>());
    [Fact] void should_checkpoint_the_handled_batch() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(first_event_sequence_number);
}
