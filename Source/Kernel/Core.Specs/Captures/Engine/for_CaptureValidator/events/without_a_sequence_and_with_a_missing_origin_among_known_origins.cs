// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class without_a_sequence_and_with_a_missing_origin_among_known_origins : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() => Register("UnknownOrigin", EventTypeVisibility.Public, string.Empty);

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(sequence: null, from: [Dispatched, "UnknownOrigin"]));

    [Fact] void should_only_report_the_missing_origin() => _result.Count().ShouldEqual(1);
    [Fact] void should_identify_the_event_without_an_origin() => _result.Single().Message.ShouldContain("'UnknownOrigin' has no origin");
}
