// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

public class and_the_step_failed_with_a_result_of_another_shape : Specification
{
    readonly JsonSerializerOptions _options = new();
    JobStateWithLastHandledEvent _state;
    Exception _error;

    void Establish() => _state = new()
    {
        LastHandledEventSequenceNumber = 3UL,
        LastScannedEventSequenceNumber = 7UL,
        HandledAllEvents = false
    };

    void Because()
    {
        using var document = JsonDocument.Parse("{}");
        _error = Catch.Exception(() => _state.HandleResult(
            JobStepResult.Failed(new PerformJobStepError(document.RootElement, ["Handler threw"], string.Empty, false)), _options));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_keep_the_handled_watermark() => _state.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_scanned_watermark() => _state.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)7UL);
    [Fact] void should_not_mark_all_events_as_handled() => _state.HandledAllEvents.ShouldBeFalse();
}
