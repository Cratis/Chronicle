// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureEventTranslator.when_translating;

public class an_event_without_a_key : given.an_events_capture_translator
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _translator.Translate(_definition, null, Content("pending"), Context(eventSourceId: string.Empty)));

    [Fact] void should_fail_explicitly() => _result.ShouldBeOfExactType<MissingKeyForCapturedEvent>();
}
