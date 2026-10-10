// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_asking_if_a_stream_is_completed;

public class and_stream_id_is_empty : given.an_event_sequence
{
    bool _result;

    void Establish() => _sequences.IsStreamScopeCompleted(Arg.Any<Contracts.Sequences.IsStreamScopeCompletedRequest>(), CallContext.Default)
        .Returns(QueryResult<Contracts.Sequences.StreamScopeCompletionResponse>.Success(Guid.NewGuid(), new() { IsCompleted = true }));

    async Task Because() => _result = await _eventSequence.IsStreamCompleted("transactions", new EventStreamId(string.Empty));

    [Fact] void should_report_completion() => _result.ShouldBeTrue();
    [Fact] async Task should_query_the_default_stream_id() => await _sequences.Received(1).IsStreamScopeCompleted(Arg.Is<Contracts.Sequences.IsStreamScopeCompletedRequest>(request => request.EventStreamType == "transactions" && request.EventStreamId == EventStreamId.Default), CallContext.Default);
}
