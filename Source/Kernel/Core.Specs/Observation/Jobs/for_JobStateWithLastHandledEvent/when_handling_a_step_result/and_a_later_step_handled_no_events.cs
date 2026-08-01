// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

/// <summary>
/// Unavailable is the largest sequence number there is, so a step that handled nothing compares as newer than
/// one that handled events. Recording it would erase what an earlier step actually did.
/// </summary>
public class and_a_later_step_handled_no_events : Specification
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    readonly EventSequenceNumber _alreadyHandled = 42UL;
    JobStateWithLastHandledEvent _state;

    void Establish()
    {
        _state = new();
        _state.HandleResult(JobStepResult.Succeeded(new HandleEventsForPartitionResult(_alreadyHandled)), _jsonSerializerOptions);
    }

    void Because() => _state.HandleResult(JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)), _jsonSerializerOptions);

    [Fact] void should_keep_the_sequence_number_that_was_handled() => _state.LastHandledEventSequenceNumber.ShouldEqual(_alreadyHandled);
    [Fact] void should_not_report_having_succeeded_without_handling_any_events() => _state.SucceededWithoutHandlingAnyEvents.ShouldBeFalse();
}
