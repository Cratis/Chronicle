// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

public class and_appending_many : given_a_scenario_with_an_event_source
{
    AppendManyResult _result;
    IEnumerable<AppendedEvent> _stored;

    async Task Because()
    {
        _result = await _scenario.EventLog.AppendMany<WarehouseEventSource>(_source, [new TestEvent("one"), new TestEvent("two")], "Receiving");
        _stored = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_both_events() => _stored.Count().ShouldEqual(2);
    [Fact] void should_record_the_event_source_on_every_event() => _stored.All(_ => _.Context.EventSource?.Value == "Warehouse").ShouldBeTrue();
    [Fact] void should_store_the_stream_type_on_every_event() => _stored.All(_ => _.Context.EventStreamType.Value == "Receiving").ShouldBeTrue();
}
