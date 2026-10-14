// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_definition_routed_events_before_stream_acquisition : given.a_protected_reader
{
    DecisionRead<Model>? _read;
    Exception _error;

    void Establish()
    {
        var sources = Substitute.For<IEventSources>();
        sources.GetFor(typeof(Created)).Returns(new EventSourceDefinition(typeof(Created), "account", "", ConcurrencyDimensions.None, [new("Transactions", "", ConcurrencyDimensions.None)]));
        _store.EventSources.Returns(sources);
        _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>()).Returns(new GetInstanceByKeyResponse
        {
            StreamScoped = true, LastHandledEventSequenceNumber = 4, ReadModel = "{\"id\":\"stream-id\"}"
        });
    }

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        _unit.AddEvents(EventSequenceId.Log, [new("source", new Created()) { EventSource = typeof(Created), EventStream = "Transactions", EventStreamId = "stream-id" }], []);
        _error = await Catch.Exception(async () => _read = await _reader.GetFor<Model>(_unit, "source", "Transactions", "stream-id", "account"));
    }

    [Fact] void should_refuse_the_stale_decision() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
    [Fact] void should_not_expose_the_read() => _read.ShouldBeNull();
}
