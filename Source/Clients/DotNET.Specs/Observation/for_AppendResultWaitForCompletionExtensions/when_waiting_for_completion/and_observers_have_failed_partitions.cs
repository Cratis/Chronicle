// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_observers_have_failed_partitions : given.an_append_result_for_completion
{
    Contracts.Observation.WaitForObserverCompletionRequest _request = null!;

    void Establish()
    {
        _observers
            .WaitForCompletion(Arg.Do<Contracts.Observation.WaitForObserverCompletionRequest>(request => _request = request), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new Contracts.Observation.WaitForObserverCompletionResponse
            {
                IsSuccess = false,
                FailedPartitions =
                [
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ObserverId = "observer-1",
                        Partition = "partition-1",
                        Attempts = []
                    }
                ]
            });
    }

    async Task Because() => _result = await _appendResult.WaitForCompletion();

    [Fact] void should_be_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_include_failed_partitions() => _result.FailedPartitions.Count().ShouldEqual(1);
    [Fact] void should_pass_event_store() => _request.EventStore.ShouldEqual(_appendResult.EventStore.Value);
    [Fact] void should_pass_namespace() => _request.Namespace.ShouldEqual(_appendResult.EventStoreNamespace.Value);
    [Fact] void should_pass_event_sequence_id() => _request.EventSequenceId.ShouldEqual(_appendResult.EventSequenceId.Value);
    [Fact] void should_pass_tail_sequence_number() => _request.TailEventSequenceNumber.ShouldEqual(_appendResult.TailSequenceNumber.Value);
}
