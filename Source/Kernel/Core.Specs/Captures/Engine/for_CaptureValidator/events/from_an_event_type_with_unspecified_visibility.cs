// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class from_an_event_type_with_unspecified_visibility : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() => Register("Legacy", EventTypeVisibility.Unspecified, "fulfillment");

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(from: ["Legacy"]));

    [Fact] void should_reject_it() => _result.Count().ShouldEqual(1);
    [Fact] void should_name_the_type() => _result.First().Message.ShouldContain("Legacy");
}
