// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class from_an_unregistered_event_type : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(from: [Dispatched, "Unknown"]));

    [Fact] void should_have_one_message() => _result.Count().ShouldEqual(1);
    [Fact] void should_name_the_unknown_event_type() => _result.First().Message.ShouldContain("Unknown");
}
