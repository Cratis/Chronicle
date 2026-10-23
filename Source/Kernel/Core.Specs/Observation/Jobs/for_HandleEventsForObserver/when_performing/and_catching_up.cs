// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_catching_up : given.a_performing_job_step
{
    void Establish() => _performState.EventObservationState = EventObservationState.Initial;

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_keep_reporting_per_run() => _observer.Received(3).ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(), Arg.Any<JobStepId>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_use_the_replay_batch_path() => _observer.DidNotReceive().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>());
}
