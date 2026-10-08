// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

/// <summary>
/// A step that did not complete may have left events behind, so nothing it read counts as read past.
/// </summary>
public class and_the_step_failed_after_reading_further : Specification
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    JobStateWithLastHandledEvent _state;

    void Establish() => _state = new();

    void Because() => _state.HandleResult(
        JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(new HandleEventsForPartitionResult(3UL) { LastScannedEventSequenceNumber = 7UL }, ["failed"], "trace")),
        _jsonSerializerOptions);

    [Fact] void should_not_record_anything_as_read() => _state.LastScannedEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
