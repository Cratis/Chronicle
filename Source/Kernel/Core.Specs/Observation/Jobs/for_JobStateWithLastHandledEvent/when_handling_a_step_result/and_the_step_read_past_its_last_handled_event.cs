// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobStateWithLastHandledEvent.when_handling_a_step_result;

/// <summary>
/// A completed step read events after the last one it handled - the observer's filters excluded them. How far it
/// read is kept apart from what it handled.
/// </summary>
public class and_the_step_read_past_its_last_handled_event : Specification
{
    readonly JsonSerializerOptions _jsonSerializerOptions = new();
    JobStateWithLastHandledEvent _state;

    void Establish() => _state = new();

    void Because() => _state.HandleResult(
        JobStepResult.Succeeded(new HandleEventsForPartitionResult(3UL) { LastScannedEventSequenceNumber = 7UL }),
        _jsonSerializerOptions);

    [Fact] void should_keep_the_last_handled_event() => _state.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_last_read_event() => _state.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)7UL);
}
