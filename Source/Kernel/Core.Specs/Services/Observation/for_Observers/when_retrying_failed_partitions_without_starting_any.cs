// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Security.given;
using FailedPartitionsHelpers = Cratis.Chronicle.Observation.FailedPartitionsHelpers;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

/// <summary>
/// The retry is accepted as a job whenever there are failed partitions; the count of them is reported without the
/// observer grain being asked, and nothing is thrown when there are none.
/// </summary>
public class when_retrying_failed_partitions_without_starting_any : given.a_replayable_observer
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task should_report_the_count_of_failed_partitions_without_throwing(int count)
    {
        var failedPartitions = new Concepts.Observation.FailedPartitions();
        for (var index = 0; index < count; index++)
        {
            FailedPartitionsHelpers.AddFailedPartition(failedPartitions, $"partition-{index}");
        }

        _failedPartitionsStorage.GetFor(Arg.Any<ObserverId>()).Returns(Task.FromResult(failedPartitions));

        var response = await _observers.RetryFailedPartitions(
            new RetryFailedPartitions { EventStore = "application", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log" },
            CallFor(new Probe(), CallerNamed(a_system_store_caller.OrdinaryUser)));

        response.FailedPartitions.ShouldEqual(count);
        await _jobsManager.Received(count > 0 ? 1 : 0).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
            Arg.Is<ObserverOperatorCommandRequest>(_ => _.Command == ObserverOperatorCommand.RetryFailedPartitions));
        await _observer.DidNotReceiveWithAnyArgs().GetFailedPartitionKeys();
    }
}
