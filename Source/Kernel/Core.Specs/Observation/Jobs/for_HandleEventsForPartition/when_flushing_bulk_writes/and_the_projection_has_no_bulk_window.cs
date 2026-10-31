// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_the_projection_has_no_bulk_window : given.a_buffered_projection_job_step
{
    void Establish() => _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(Task.FromResult(Windows()));

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_succeed() => _result.Match(result => !result.TryGetError(out _), _ => false).ShouldBeTrue();
    [Fact] void should_checkpoint_the_handled_batch() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(1UL);
}
