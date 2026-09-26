// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_a_mixed_batch_is_appended : given.an_append_result_for_completion
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
            SequenceNumbers = [40UL, 41UL, 42UL],
            EventTypes = [a, b],
            AppendedEventTypes = [a, b, a],
            Observers = _observers
        };
        _observers.WaitForCompletion(Arg.Do<Contracts.Observation.WaitForObserverCompletionRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns(new Contracts.Observation.WaitForObserverCompletionResponse { IsSuccess = true });
    }

    async Task Because() => _result = await _batch.WaitForCompletion();

    [Fact] void should_send_the_last_number_for_each_type() => _request.EventTypeTails.Single(_ => _.EventType.Id == "a-recorded").SequenceNumber.ShouldEqual(42UL);
    [Fact] void should_send_the_other_type_number() => _request.EventTypeTails.Single(_ => _.EventType.Id == "b-recorded").SequenceNumber.ShouldEqual(41UL);
}
