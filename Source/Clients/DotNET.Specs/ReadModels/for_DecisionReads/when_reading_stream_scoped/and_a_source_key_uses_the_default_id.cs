// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_stream_scoped;

public class and_a_source_key_uses_the_default_id : given.a_decision_reader
{
    DecisionRead<Model> _result;

    void Establish() => _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>()).Returns(new GetInstanceByKeyResponse
    {
        StreamScoped = true, LastHandledEventSequenceNumber = 4, ReadModel = "{\"id\":\"source\"}"
    });

    async Task Because() => _result = await _reader.GetDetached<Model>("source", "stream-type", EventStreamId.Default);

    [Fact] void should_allow_a_specific_type_with_default_id() => _result.IsProtected.ShouldBeTrue();
    [Fact] void should_keep_the_source_key() => _result.Key.Value.ShouldEqual("source");
    [Fact] void should_keep_the_specific_stream_type_in_the_guard() => _result.EventStreamType!.Value.ShouldEqual("stream-type");
    [Fact] void should_keep_the_default_stream_id_in_the_guard() => _result.EventStreamId!.IsDefault.ShouldBeTrue();
}
