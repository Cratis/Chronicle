// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class from_a_public_event_type_without_an_origin : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() => Register("Local", EventTypeVisibility.Public, string.Empty);

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(from: ["Local"]));

    [Fact] void should_reject_it() => _result.Count().ShouldEqual(1);
    [Fact] void should_say_it_does_not_originate_from_another_store() => _result.First().Message.ShouldContain("does not originate");
}
