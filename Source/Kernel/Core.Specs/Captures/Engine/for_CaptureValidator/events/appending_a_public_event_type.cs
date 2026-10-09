// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class appending_a_public_event_type : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() => Register("Published", EventTypeVisibility.Public, string.Empty);

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(appends: "Published"));

    [Fact] void should_have_one_message() => _result.Count().ShouldEqual(1);
    [Fact] void should_say_the_type_is_public() => _result.First().Message.ShouldContain("public");
}
