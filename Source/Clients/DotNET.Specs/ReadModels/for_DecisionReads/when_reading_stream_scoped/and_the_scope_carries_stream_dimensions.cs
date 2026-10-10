// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_stream_scoped;

public class and_the_scope_carries_stream_dimensions : given.a_decision_reader
{
    DecisionRead<Model> _result;

    void Establish()
    {
        _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>()).Returns(new GetInstanceByKeyResponse
        {
            StreamScoped = true, LastHandledEventSequenceNumber = 4, ReadModel = "{\"id\":\"stream-id\"}"
        });
    }

    async Task Because() => _result = await _reader.GetDetached<Model>("source", "stream-type", "stream-id", "source-type");

    [Fact] void should_use_the_stream_id_as_the_model_key() => _result.Key.Value.ShouldEqual("stream-id");
    [Fact] void should_guard_the_source_id_not_the_model_key() => ((IDecisionRead)_result).Scope.EventSourceId!.Value.ShouldEqual("source");
    [Fact] void should_guard_the_stream_type() => _result.EventStreamType!.Value.ShouldEqual("stream-type");
    [Fact] void should_guard_the_stream_id() => _result.EventStreamId!.Value.ShouldEqual("stream-id");
    [Fact] void should_guard_the_source_type() => ((IDecisionRead)_result).Scope.EventSourceType!.Value.ShouldEqual("source-type");
    [Fact] void should_narrow_the_probe() => _sequences.Received(1).TailSequenceNumber(Arg.Is<TailSequenceNumberRequest>(_ => _.EventSourceId == "source" && _.EventStreamId == "stream-id" && _.EventStreamType == "stream-type" && _.EventSourceType == "source-type"), Arg.Any<CallContext>());
    [Fact] void should_narrow_the_fold() => _readModels.Received(1).GetInstanceByKey(Arg.Is<GetInstanceByKeyRequest>(_ => _.EventSourceId == "source" && _.ReadModelKey == "stream-id" && _.EventStreamId == "stream-id" && _.EventStreamType == "stream-type" && _.EventSourceType == "source-type"), Arg.Any<CallContext>());
    [Fact] void should_dehydrate_the_same_scoped_session() => _readModels.Received(1).DehydrateSession(Arg.Is<DehydrateSessionRequest>(_ => _.EventSourceId == "source" && _.ReadModelKey == "stream-id" && _.EventStreamId == "stream-id" && _.EventStreamType == "stream-type" && _.EventSourceType == "source-type"), Arg.Any<CallContext>());
}
