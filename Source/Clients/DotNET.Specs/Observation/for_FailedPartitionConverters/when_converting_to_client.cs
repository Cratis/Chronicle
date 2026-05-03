// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.for_FailedPartitionConverters;

public class when_converting_to_client : Specification
{
    Contracts.Observation.FailedPartition _contract;
    FailedPartition _result;
    Guid _failedPartitionId;
    DateTimeOffset _attemptOccurred;

    void Establish()
    {
        _failedPartitionId = Guid.NewGuid();
        _attemptOccurred = DateTimeOffset.UtcNow;
        _contract = new Contracts.Observation.FailedPartition
        {
            Id = _failedPartitionId,
            ObserverId = "my-observer",
            Partition = "partition-42",
            Attempts =
            [
                new Contracts.Observation.FailedPartitionAttempt
                {
                    Occurred = _attemptOccurred,
                    SequenceNumber = 7,
                    Messages = ["error one", "error two"],
                    StackTrace = "at SomeMethod"
                }
            ]
        };
    }

    void Because() => _result = _contract.ToClient();

    [Fact] void should_set_id() => _result.Id.ShouldEqual(new FailedPartitionId(_failedPartitionId));
    [Fact] void should_set_observer_id() => _result.ObserverId.ShouldEqual(new ObserverId("my-observer"));
    [Fact] void should_set_partition() => _result.Partition.ShouldEqual(new Partition("partition-42"));
    [Fact] void should_have_one_attempt() => _result.Attempts.Count().ShouldEqual(1);
    [Fact] void should_set_attempt_occurred() => _result.Attempts.First().Occurred.ShouldEqual(_attemptOccurred);
    [Fact] void should_set_attempt_sequence_number() => _result.Attempts.First().SequenceNumber.ShouldEqual(new EventSequenceNumber(7));
    [Fact] void should_set_attempt_messages() => _result.Attempts.First().Messages.ShouldContainOnly(["error one", "error two"]);
    [Fact] void should_set_attempt_stack_trace() => _result.Attempts.First().StackTrace.ShouldEqual("at SomeMethod");
}
