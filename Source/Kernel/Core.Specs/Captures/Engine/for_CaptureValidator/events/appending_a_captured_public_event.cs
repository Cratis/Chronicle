// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class appending_a_captured_public_event : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(appends: Dispatched));

    [Fact] void should_refuse_appending_a_public_event() => _result.Any(_ => _.Message.Contains("cannot also be appended")).ShouldBeTrue();
}
