// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Security.given;
using IObserver = Cratis.Chronicle.Observation.IObserver;
using RecoveryOutcome = Cratis.Chronicle.Concepts.Observation.PartitionRecoveryOutcome;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_retrying_failed_partitions_without_starting_any : given.all_dependencies
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(RecoveryOutcome.PartitionQuarantined);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task should_preserve_the_existing_rpc_count_without_throwing(int count)
    {
        var partitions = Enumerable.Range(0, count).Select(_ => (Key)$"partition-{_}").ToArray();
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>(partitions));

        var response = await _observers.RetryFailedPartitions(
            new RetryFailedPartitions { EventStore = "application", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log" },
            CallFor(new Probe(), CallerNamed(a_system_store_caller.OrdinaryUser)));

        response.FailedPartitions.ShouldEqual(count);
        await _observer.Received(count).TryStartRecoverJobForFailedPartition(Arg.Any<Key>());
    }
}
