// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_a_mixed_batch_starts_at_zero : given.an_append_result_for_completion
{
    Contracts.Observation.WaitForObserverCompletionRequest _request = null!;
    AppendManyResult _batch;

    void Establish()
    {
        var a = new EventType("a-recorded", 1);
        var b = new EventType("b-recorded", 1);
        _batch = new AppendManyResult
        {
            EventStore = "event-store",
            EventStoreNamespace = "event-store-namespace",
            EventSequenceId = EventSequenceId.Log,
            SequenceNumbers = [0UL, 1UL],
            EventTypes = [a, b],
            AppendedEventTypes = [a, b],
            Observers = _observers
        };
        _observers.WaitForCompletion(Arg.Do<Contracts.Observation.WaitForObserverCompletionRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns(new Contracts.Observation.WaitForObserverCompletionResponse { IsSuccess = true });
    }

    async Task Because() => _result = await _batch.WaitForCompletion();

    [Fact] void should_send_the_first_sequence_number_even_when_zero() => _request.FirstEventSequenceNumber.ShouldEqual(0UL);
    [Fact] void should_mark_the_first_number_as_supplied() => _request.HasFirstEventSequenceNumber.ShouldBeTrue();
}
