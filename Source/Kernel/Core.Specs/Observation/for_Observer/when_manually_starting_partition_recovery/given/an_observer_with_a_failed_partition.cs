// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_manually_starting_partition_recovery.given;

public class an_observer_with_a_failed_partition : for_Observer.given.an_observer_with_subscription
{
    protected const string Partition = "some-partition";
    protected FailedPartition _failure;
    protected PartitionRecoveryOutcome _outcome;
    readonly ILogger<Observer> _recoveryLogger = Substitute.For<ILogger<Observer>>();

    protected override ILogger<Observer> CreateLogger()
    {
        _recoveryLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return _recoveryLogger;
    }

    void Establish()
    {
        _failure = new() { Partition = (Key)Partition };
        _failedPartitionsState.Partitions = [_failure];
        _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _jobsManager.ClearReceivedCalls();
    }

    protected bool LoggedReason(string reason) => _recoveryLogger.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
        .Select(call => call.GetArguments()[2])
        .OfType<IEnumerable<KeyValuePair<string, object?>>>()
        .Any(state => state.Any(value => value.Key == "Partition" && Equals(value.Value, (Key)Partition)) &&
            state.Any(value => value.Key == "Reason" && Equals(value.Value, reason)));
}
