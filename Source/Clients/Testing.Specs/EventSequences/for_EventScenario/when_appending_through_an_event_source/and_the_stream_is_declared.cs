// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

public class and_the_stream_is_declared : given_a_scenario_with_an_event_source
{
    AppendResult _result;
    AppendedEvent _stored;

    async Task Because()
    {
        _result = await _scenario.EventLog.Append<WarehouseEventSource>(_source, new TestEvent("hello"), "Receiving", "dock-3");
        _stored = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source)).Single();
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_store_the_event_source_type_from_the_definition() => _stored.Context.EventSourceType.Value.ShouldEqual("Warehouse");
    [Fact] void should_store_the_stream_type_from_the_definition() => _stored.Context.EventStreamType.Value.ShouldEqual("Receiving");
    [Fact] void should_store_the_stream_id_as_given() => _stored.Context.EventStreamId.Value.ShouldEqual("dock-3");
    [Fact] void should_record_the_event_source_on_the_context() => _stored.Context.EventSource!.Value.ShouldEqual("Warehouse");
}
