// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureEventTranslator.when_translating;

public class an_event_that_does_not_change_anything : given.an_events_capture_translator
{
    CaptureEventTranslation _result;

    void Because() => _result = _translator.Translate(_definition, Content("dispatched"), Content("dispatched"), Context(sequenceNumber: 3));

    [Fact] void should_not_append_anything() => _result.Events.ShouldBeEmpty();
}
