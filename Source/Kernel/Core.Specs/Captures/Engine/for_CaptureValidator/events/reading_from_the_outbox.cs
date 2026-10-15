// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class reading_from_the_outbox : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(sequence: "outbox"));

    [Fact] void should_say_it_is_not_an_inbox() => _result.Any(_ => _.Message.Contains("is not an inbox")).ShouldBeTrue();
}
