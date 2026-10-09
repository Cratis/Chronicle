// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureEventTranslator.when_translating;

public class the_first_event_for_a_key : given.an_events_capture_translator
{
    CaptureEventTranslation _result;

    void Because() => _result = _translator.Translate(_definition, null, Content("pending"), Context());

    [Fact] void should_key_on_the_event_source_id_of_the_event() => _result.Key.ShouldEqual("shipment-1");
    [Fact] void should_remember_the_event_content() => _result.Current["status"]!.ToString().ShouldEqual("pending");
    [Fact] void should_not_append_a_transition_that_has_not_happened() => _result.Events.ShouldBeEmpty();
}
