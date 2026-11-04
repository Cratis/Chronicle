// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.Copy.for_CopyEventSourceEventsStep;

public class when_preparing : given.the_job_step
{
    protected override IEnumerable<EventTypeId> RequestedEventTypes => [new("copied-event")];

    async Task Because() => await _jobStep.Prepare(_request);

    [Fact] void should_set_source_in_state() => _stateStorage.State.SourceId.ShouldEqual(_sourceId);
    [Fact] void should_set_target_in_state() => _stateStorage.State.TargetId.ShouldEqual(_targetId);
    [Fact] void should_set_event_sequence_in_state() => _stateStorage.State.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_set_event_types_in_state() => _stateStorage.State.EventTypes.ShouldContainOnly(new EventTypeId("copied-event"));
}
