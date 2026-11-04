// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_a_step_fails;

public class and_it_threw_without_a_result : given.a_replay_observer_job
{
    Exception _error;
    bool _retries;

    void Establish() => _stateStorage.State.LastHandledEventSequenceNumber = 42UL;

    async Task Because()
    {
        await _job.Start(_request);
        var steps = await _job.PrepareStepsForTesting(_request);
        var result = JobStepResult.Failed(new PerformJobStepError(string.Empty, ["Handler threw"], string.Empty, false));
        _error = await Catch.Exception(() => _job.RecordStepForTesting(steps[0].Id, result));
        _retries = _job.ShouldRetryStepForTesting(steps[0].Id, result, 1);
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_not_retry() => _retries.ShouldBeFalse();
    [Fact] async Task should_not_report_a_partition_failure() => await _observer.DidNotReceiveWithAnyArgs().PartitionFailedWithDeliveryFence(
        Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>(), Arg.Any<PartitionFailureContext>());
    [Fact] void should_keep_the_handled_watermark() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
