// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

/// <summary>
/// One step handled events, another read only events the filters exclude. The furthest read is recorded even though
/// the later step handled nothing, and the handled event is left as it was.
/// </summary>
public class and_a_step_that_handled_nothing_read_past_an_earlier_one : Specification
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    JobStateWithLastHandledEvent _state;

    void Establish()
    {
        _state = new();
        _state.HandleResult(JobStepResult.Succeeded(new HandleEventsForPartitionResult(3UL) { LastScannedEventSequenceNumber = 3UL }), _jsonSerializerOptions);
    }

    void Because() => _state.HandleResult(
        JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable) { LastScannedEventSequenceNumber = 9UL }),
        _jsonSerializerOptions);

    [Fact] void should_keep_the_last_handled_event() => _state.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_record_the_furthest_read_event() => _state.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)9UL);
}
