// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_tail_sequence_number_is_unavailable : Specification
{
    Contracts.Observation.IObservers _observers;
    AppendResult _appendResult;
    AppendResultWaitForCompletionResult _result;

    void Establish()
    {
        _observers = Substitute.For<Contracts.Observation.IObservers>();
        _appendResult = new AppendResult
        {
            SequenceNumber = Events.EventSequenceNumber.Unavailable,
            Observers = _observers
        };
    }

    async Task Because() => _result = await _appendResult.WaitForCompletion();

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_have_failed_partitions() => _result.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_not_call_observer_service() => _observers.DidNotReceive().WaitForCompletion(
        Arg.Any<Contracts.Observation.WaitForObserverCompletionRequest>(),
        Arg.Any<ProtoBuf.Grpc.CallContext>());
}
