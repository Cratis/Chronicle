// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_the_pinned_window_is_kept : given.a_debouncing_job_step
{
    async Task Establish()
    {
        var window = Guid.NewGuid();
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
            Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [window] }),
            Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [window] }));
        await _jobStep.FlushCheckpoint(start: true);
        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
    }

    async Task Because() => await _jobStep.ReportNewSuccessfullyHandledEvent(3UL);

    [Fact] void should_advance_the_durable_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
