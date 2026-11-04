// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class with_all_filters : given.an_asynchronous_event_sequence
{
    EventSequenceNumber _result;
    EventType[] _types;

    void Establish() => _types = [new("tail-type-one", EventTypeGeneration.First), new("tail-type-two", EventTypeGeneration.First)];

    async Task Because() => _result = await _eventSequence.GetTailSequenceNumberWithCancellation(
        _cancellation.Token, "source", "source-type", "stream-type", "stream", _types).WaitAsync(TimeSpan.FromSeconds(10));

    [Fact] void should_forward_the_exact_acquisition_token() => _acquisitionToken.ShouldEqual(_cancellation.Token);
    [Fact] void should_forward_the_exact_rpc_token() => _rpcToken.ShouldEqual(_cancellation.Token);
    [Fact] void should_return_the_tail() => _result.ShouldEqual((EventSequenceNumber)75UL);
    [Fact] void should_call_the_rpc_once() => _rpcCalls.ShouldEqual(1);
    [Fact] void should_not_acquire_synchronously() => _ = _accessor.DidNotReceive().Services;
    [Fact] void should_preserve_the_store() => _request.EventStore.ShouldEqual("tail-store");
    [Fact] void should_preserve_the_namespace() => _request.Namespace.ShouldEqual("tail-namespace");
    [Fact] void should_preserve_the_sequence() => _request.EventSequenceId.ShouldEqual("tail-sequence");
    [Fact] void should_preserve_the_source() => _request.EventSourceId.ShouldEqual("source");
    [Fact] void should_preserve_the_source_type() => _request.EventSourceType.ShouldEqual("source-type");
    [Fact] void should_preserve_the_stream_type() => _request.EventStreamType.ShouldEqual("stream-type");
    [Fact] void should_preserve_the_stream() => _request.EventStreamId.ShouldEqual("stream");
    [Fact] void should_preserve_the_event_types() => _request.EventTypeIds.ShouldEqual(string.Join(',', _types.Select(_ => _.Id.Value)));
}
