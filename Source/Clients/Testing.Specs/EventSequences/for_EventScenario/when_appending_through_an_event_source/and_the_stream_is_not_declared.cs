// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

public class and_the_stream_is_not_declared : given_a_scenario_with_an_event_source
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _scenario.EventLog.Append<WarehouseEventSource>(_source, new TestEvent("hello"), "Shipping"));

    [Fact] void should_reject_the_append() => _exception.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
}
