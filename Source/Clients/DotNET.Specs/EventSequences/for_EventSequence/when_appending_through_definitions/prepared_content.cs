// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class prepared_content : given.a_definition_backed_batch
{
    PreparedEvent _prepared;
    Contracts.Sequences.AppendRequest _append;

    async Task Establish()
    {
        var json = new JsonObject { ["providerValue"] = "first" };
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(json);
        _prepared = await _eventSequence.Prepare("item");
        json["providerValue"] = "changed";
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _append = call.Arg<Contracts.Sequences.AppendRequest>();
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 42, ConstraintViolations = [], Errors = [] });
        });
    }

    async Task Because() => await _eventSequence.AppendPrepared<EventSources.for_EventSources.ShoppingCartEventSource>(_sourceId, _prepared, "Items", "2025-01");

    [Fact] void should_serialize_only_once() => _eventSerializer.Received(1).Serialize("item");
    [Fact] void should_append_the_snapshot() => _append.Content.ShouldEqual("{\"providerValue\":\"first\"}");
    [Fact] void should_append_through_the_event_source() => _append.EventSource.ShouldEqual("ShoppingCart");
    [Fact] void should_append_to_the_declared_stream() => _append.EventStreamType.ShouldEqual("Items");
    [Fact] void should_narrow_the_scope_to_the_stream() => _append.ConcurrencyScope.EventStreamId.ShouldEqual("2025-01");
}
