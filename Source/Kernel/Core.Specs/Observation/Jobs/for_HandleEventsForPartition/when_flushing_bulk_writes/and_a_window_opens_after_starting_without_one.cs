// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_a_window_opens_after_starting_without_one : given.a_buffered_projection_job_step
{
    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
        Task.FromResult(Windows()), Task.FromResult(Windows(_window)));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => _result.Match(result => result.TryGetError(out _), _ => true).ShouldBeTrue();
    [Fact] void should_keep_the_prior_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_count_the_batch() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(0);
}
