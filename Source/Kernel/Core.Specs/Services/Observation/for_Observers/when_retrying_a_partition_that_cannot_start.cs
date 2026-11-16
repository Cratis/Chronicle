// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Security.given;
using IObserver = Cratis.Chronicle.Observation.IObserver;
using RecoveryOutcome = Cratis.Chronicle.Concepts.Observation.PartitionRecoveryOutcome;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_retrying_a_partition_that_cannot_start : given.all_dependencies
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    [Theory]
    [InlineData(RecoveryOutcome.PartitionNotFound)]
    [InlineData(RecoveryOutcome.ObserverQuarantined)]
    [InlineData(RecoveryOutcome.PartitionQuarantined)]
    public async Task should_return_the_existing_rpc_outcome_without_throwing(RecoveryOutcome outcome)
    {
        _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(outcome);

        var response = await _observers.RetryPartition(
            new RetryPartition { EventStore = "application", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log", Partition = "partition" },
            CallFor(new Probe(), CallerNamed(a_system_store_caller.OrdinaryUser)));

        response.Outcome.ShouldEqual((PartitionRecoveryOutcome)(int)outcome);
    }
}
