// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing_an_isolated_rebuild;

public class and_publish_is_superseded : given.a_performing_isolated_rebuild
{
    void Establish() => _rebuilder.Publish(Arg.Any<EventSequenceNumber>()).Returns(false);

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => StepResult.TryGetError(out _).ShouldBeTrue();

    [Fact]
    void should_fail_transiently_so_the_job_runs_it_again()
    {
        TransientTimeouts.TryGetTransientFailure(StepResult, new JsonSerializerOptions(), out var failure).ShouldBeTrue();
        failure.Messages.ShouldContain(HandleEventsForPartition.IsolatedRebuildSuperseded);
    }

    [Fact] void should_report_nothing_handled() => HandledResult()!.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_report_the_partition_as_failed() => _observer.DidNotReceiveWithAnyArgs().PartitionFailed(default!, default!, default!, default!, default);
}
