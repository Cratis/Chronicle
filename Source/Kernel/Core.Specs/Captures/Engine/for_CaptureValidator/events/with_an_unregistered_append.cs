// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class with_an_unregistered_append : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(appends: "Missing"));

    [Fact] void should_name_the_unknown_append() => _result.Any(_ => _.Message.Contains("Missing")).ShouldBeTrue();
}
