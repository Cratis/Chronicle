// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class with_a_poll_interval : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(poll: "5m"));

    [Fact] void should_refuse_polling() => _result.Any(_ => _.Message.Contains("not polled")).ShouldBeTrue();
}
