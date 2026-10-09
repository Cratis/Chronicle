// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_subscribing;

public class a_definition_without_an_events_source : given.a_capture_events_subscriptions
{
    Exception _exception;

    void Establish() => _definition = _definition with { Source = new SourceDefinition(SourceType.Api, Api: "x", Poll: "5m") };

    async Task Because() => _exception = await Catch.Exception(() => _subscriptions.Subscribe(_eventStore, _definition));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<UnsupportedCaptureCapability>();
}
