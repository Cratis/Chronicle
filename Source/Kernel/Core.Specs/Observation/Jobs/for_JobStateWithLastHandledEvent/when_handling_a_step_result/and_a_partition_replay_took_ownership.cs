// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

public class and_a_partition_replay_took_ownership : Specification
{
    JobStateWithLastHandledEvent _state;

    void Establish() => _state = new();

    void Because()
    {
        var result = JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable) { FencedByPartitionReplay = true });
        _state.HandleResult(result, new JsonSerializerOptions());
    }

    [Fact] void should_preserve_replay_ownership_in_persisted_job_state() => _state.FencedByPartitionReplay.ShouldBeTrue();
    [Fact] void should_not_advance_the_handled_position() => _state.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
