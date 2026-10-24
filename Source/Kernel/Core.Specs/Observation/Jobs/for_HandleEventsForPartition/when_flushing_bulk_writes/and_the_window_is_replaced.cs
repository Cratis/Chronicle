// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_the_window_is_replaced : given.a_buffered_projection_job_step
{
    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
        Task.FromResult(Windows(_window)), Task.FromResult(Windows(Guid.NewGuid())));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => _result.Match(result => result.TryGetError(out _), _ => true).ShouldBeTrue();
    [Fact] void should_keep_the_prior_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue.ShouldBeFalse();
    [Fact] void should_record_the_partition_failed_from_the_unconfirmed_range() => _observer.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed) &&
        Equals(call.GetArguments()[1], _performState.StartEventSequenceNumber) && Equals(call.GetArguments()[4], FailureKind.Handling)).ShouldEqual(1);
    [Fact] void should_not_count_the_batch() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(0);
}
