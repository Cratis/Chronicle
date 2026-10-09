// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureEventTranslator.when_translating;

public class an_event_that_changes_the_status : given.an_events_capture_translator
{
    CaptureEventTranslation _result;

    void Because() => _result = _translator.Translate(_definition, Content("pending"), Content("dispatched"), Context(sequenceNumber: 2));

    [Fact] void should_append_one_event() => _result.Events.Count.ShouldEqual(1);
    [Fact] void should_append_the_matching_private_event() => _result.Events[0].Append.EventType.ShouldEqual("OrderShipped");
    [Fact] void should_map_the_incoming_event_content() => _result.Events[0].Content["orderId"]!.ToString().ShouldEqual("order-1");
    [Fact] void should_map_the_event_context() => _result.Events[0].Content["correlation"]!.ToString().ShouldEqual("correlation-1");
    [Fact] void should_remember_the_new_status() => _result.Current["status"]!.ToString().ShouldEqual("dispatched");
}
