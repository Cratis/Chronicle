// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

/// <summary>
/// A step that succeeded having read nothing leaves the same HandledAllEvents as one that handled everything,
/// so on its own that flag cannot tell a caller whether any work was done.
/// </summary>
public class and_the_step_handled_no_events : Specification
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    JobStateWithLastHandledEvent _state;

    void Establish() => _state = new();

    void Because() => _state.HandleResult(JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)), _jsonSerializerOptions);

    [Fact] void should_report_having_succeeded_without_handling_any_events() => _state.SucceededWithoutHandlingAnyEvents.ShouldBeTrue();
}
