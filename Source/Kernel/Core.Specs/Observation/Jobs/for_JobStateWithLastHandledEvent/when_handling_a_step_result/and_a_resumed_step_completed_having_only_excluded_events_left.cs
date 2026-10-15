// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

/// <summary>
/// A stopped step handled up to 3, and on resume every remaining event is excluded by the filters. The step completes
/// with the watermark it already had, which must still count as completion - the handled watermark does not move.
/// </summary>
public class and_a_resumed_step_completed_having_only_excluded_events_left : Specification
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    JobStateWithLastHandledEvent _state;

    void Establish()
    {
        _state = new();
        _state.HandleResult(
            JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(new HandleEventsForPartitionResult(3UL), ["stopped"], "trace")),
            _jsonSerializerOptions);
    }

    void Because() => _state.HandleResult(
        JobStepResult.Succeeded(new HandleEventsForPartitionResult(3UL) { LastScannedEventSequenceNumber = 7UL }),
        _jsonSerializerOptions);

    [Fact] void should_record_the_step_as_having_completed() => _state.HandledAllEvents.ShouldBeTrue();
    [Fact] void should_keep_the_last_handled_event() => _state.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_record_how_far_it_read() => _state.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)7UL);
}
