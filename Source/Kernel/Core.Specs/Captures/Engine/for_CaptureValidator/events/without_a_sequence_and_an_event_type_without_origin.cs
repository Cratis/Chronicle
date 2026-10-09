// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class without_a_sequence_and_an_event_type_without_origin : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() => Register("Local", EventTypeVisibility.Public, string.Empty);

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(sequence: null, from: ["Local"]));

    [Fact] void should_report_the_missing_origin() => _result.Any(_ => _.Message.Contains("do not originate")).ShouldBeTrue();
}
