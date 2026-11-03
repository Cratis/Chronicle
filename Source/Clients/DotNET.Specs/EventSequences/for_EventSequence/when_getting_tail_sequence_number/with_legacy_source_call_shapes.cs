// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number;

public class with_legacy_source_call_shapes : given.an_event_sequence
{
    List<Contracts.Sequences.TailSequenceNumberRequest> _requests;
    List<EventSequenceNumber> _results;

    void Establish()
    {
        _requests = [];
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default).Returns(call =>
        {
            _requests.Add(call.Arg<Contracts.Sequences.TailSequenceNumberRequest>());
            return QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 75UL });
        });
    }

    async Task Because()
    {
        IEventSequence view = _eventSequence;
        EventType[] types = [new("type", EventTypeGeneration.First)];
        _results =
        [
            await _eventSequence.GetTailSequenceNumber(default).WaitAsync(TimeSpan.FromSeconds(10)),
            await view.GetTailSequenceNumber(default).WaitAsync(TimeSpan.FromSeconds(10)),
            await _eventSequence.GetTailSequenceNumber("source", "source-type", "stream-type", "stream", types).WaitAsync(TimeSpan.FromSeconds(10)),
            await view.GetTailSequenceNumber("source", "source-type", "stream-type", "stream", types).WaitAsync(TimeSpan.FromSeconds(10))
        ];
    }

    [Fact] void should_execute_all_old_call_shapes() => _results.Count.ShouldEqual(4);
    [Fact] void should_return_the_tail_for_all_call_shapes() => _results.TrueForAll(_ => _.Value == 75UL).ShouldBeTrue();
    [Fact] void should_preserve_the_default_literal() => _requests.Take(2).All(_ => _.EventSourceId is null && _.EventSourceType is null && _.EventStreamType is null && _.EventStreamId is null && _.EventTypeIds is null).ShouldBeTrue();
    [Fact] void should_preserve_all_five_filters() => _requests.Skip(2).All(_ => _.EventSourceId == "source" && _.EventSourceType == "source-type" && _.EventStreamType == "stream-type" && _.EventStreamId == "stream" && _.EventTypeIds == "type").ShouldBeTrue();
}
