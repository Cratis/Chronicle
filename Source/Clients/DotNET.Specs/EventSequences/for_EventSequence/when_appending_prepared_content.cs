// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_appending_prepared_content : given.an_event_sequence_with_metadata
{
    PreparedEvent _prepared;
    Contracts.Sequences.VerifyContentRequest _verification;
    ContentVerificationResult _result;

    async Task Establish()
    {
        var json = new JsonObject { ["providerValue"] = "first" };
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(json);
        _prepared = await _eventSequence.Prepare("event");
        json["providerValue"] = "changed";
        _sequences.VerifyContent(Arg.Any<Contracts.Sequences.VerifyContentRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _verification = call.Arg<Contracts.Sequences.VerifyContentRequest>();
            return CommandResult<Contracts.Sequences.VerifyContentResponse>.Success(_correlationId, new() { Result = Contracts.Sequences.ContentVerificationResult.Equal });
        });
        await _eventSequence.AppendPrepared(_source, _prepared);
    }

    async Task Because() => _result = await _eventSequence.VerifyContent(42UL, _prepared, _source);

    [Fact] void should_serialize_only_once() => _eventSerializer.Received(1).Serialize("event");
    [Fact] void should_append_the_snapshot() => _request.Content.ShouldEqual("{\"providerValue\":\"first\"}");
    [Fact] void should_verify_the_exact_append_content() => _verification.Content.ShouldEqual(_request.Content);
    [Fact] void should_use_the_same_type_and_generation() => _verification.EventType.Generation.ShouldEqual(_request.EventType.Generation);
    [Fact] void should_require_the_requested_source() => _verification.EventSourceId.ShouldEqual(_source.Value);
    [Fact] void should_return_the_kernel_verdict() => _result.ShouldEqual(ContentVerificationResult.Equal);
}
