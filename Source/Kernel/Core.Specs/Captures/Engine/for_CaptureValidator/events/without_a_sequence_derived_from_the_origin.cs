// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.events;

public class without_a_sequence_derived_from_the_origin : an_events_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(_eventStore, EventsCapture(sequence: null));

    [Fact] void should_be_valid() => _result.ShouldBeEmpty();
}
