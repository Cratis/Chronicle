// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class without_a_sequence_and_with_several_origins : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() => Register("Foreign", EventTypeVisibility.Public, "billing");

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(sequence: null, from: [Dispatched, "Foreign"]));

    [Fact] void should_have_one_message() => _result.Count().ShouldEqual(1);
    [Fact] void should_say_there_are_several_origins() => _result.First().Message.ShouldContain("several event stores");
}
