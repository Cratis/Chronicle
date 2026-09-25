// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_round_tripping_append_metadata;

public class and_batch_append_supplies_explicit_metadata : Specification, IDisposable
{
    EventScenario _scenario;
    readonly EventSourceId _source = "source";
    IAppendResult _result;
    IImmutableList<AppendedEvent> _stored;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _result = await _scenario.EventLog.AppendMany(_source, [new TestEvent("first"), new TestEvent("second")], eventStreamType: "Payments", eventStreamId: "September", eventSourceType: "Account");
        _stored = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source);
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_store_every_event() => _stored.Count.ShouldEqual(2);
    [Fact] void should_store_the_expected_source_type() => _stored.All(_ => _.Context.EventSourceType == new EventSourceType("Account")).ShouldBeTrue();
    [Fact] void should_store_the_expected_stream_type() => _stored.All(_ => _.Context.EventStreamType == new EventStreamType("Payments")).ShouldBeTrue();
    [Fact] void should_store_the_expected_stream_id() => _stored.All(_ => _.Context.EventStreamId == new EventStreamId("September")).ShouldBeTrue();

    public void Dispose() => _scenario.Dispose();
}
