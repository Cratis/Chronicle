// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_a_catchup_flush_loses_the_window : given.a_buffered_projection_job_step
{
    void Establish()
    {
        _performState.EventObservationState = EventObservationState.Initial;
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), false).Returns(
            Task.FromResult(Windows(_window)), Task.FromResult(Windows()));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fail_the_step() => _result.Match(result => result.TryGetError(out _), _ => true).ShouldBeTrue();
    [Fact] void should_keep_the_prior_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_flush_a_replay() => _observerServiceClient.DidNotReceive().FlushAllFor(Arg.Any<ObserverDetails>(), true);
}
