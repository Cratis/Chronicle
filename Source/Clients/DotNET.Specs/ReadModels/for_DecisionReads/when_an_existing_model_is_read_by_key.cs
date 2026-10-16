// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_an_existing_model_is_read_by_key : given.a_decision_reader
{
    DecisionRead<Model> _read;

    async Task Because() => _read = await _reader.GetDetached<Model>("source");

    [Fact] void should_remain_admitted() => _reader.Admit<Model>().IsAdmitted.ShouldBeTrue();
    [Fact] void should_keep_the_same_instance() => _read.Instance.ShouldEqual(new Model("source"));
    [Fact] void should_leave_all_stream_filters_empty() => _readModels.Received(1).GetInstanceByKey(Arg.Is<GetInstanceByKeyRequest>(request => request.ReadModelKey == "source" && request.EventSourceId == string.Empty && request.EventSourceType == string.Empty && request.EventStreamType == string.Empty && request.EventStreamId == string.Empty), Arg.Any<CallContext>());
}
