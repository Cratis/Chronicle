// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class with_shared_correlation_and_distinct_positions : given.an_event_sequence
{
    IImmutableList<AppendedEvent> _result;
    Contracts.Sequences.FromSequenceNumberRequest _request;
    CorrelationId _correlation;

    void Establish()
    {
        _correlation = CorrelationId.New();
        var type = new EventType("measured", 1);
        _eventTypes.HasFor(type.Id).Returns(true);
        _eventTypes.GetClrTypeFor(type.Id).Returns(typeof(Measured));
        var events = new[] { 101UL, 102UL }.Select(position => new Contracts.Sequences.AppendedEventResponse
        {
            Context = new()
            {
                EventType = type.ToSequencesContract(), SequenceNumber = position, EventSourceId = "source",
                EventSourceType = EventSourceType.Default, EventStreamType = EventStreamType.All,
                EventStreamId = EventStreamId.Default, CorrelationId = _correlation, Causation = [],
                CausedBy = new(), Tags = []
            },
            Content = "{}"
        }).ToArray();
        _sequences.FromSequenceNumber(Arg.Any<Contracts.Sequences.FromSequenceNumberRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.FromSequenceNumberRequest>();
            return QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.NewGuid(), events);
        });
    }

    async Task Because() => _result = await _eventSequence.GetFromSequenceNumber(101);

    [Fact] void should_keep_both_foreign_and_own_positions_for_the_caller() => _result.Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([101UL, 102UL]);
    [Fact] void should_preserve_the_shared_correlation_without_filtering() => _result.Select(_ => _.Context.CorrelationId).ShouldEqual([_correlation, _correlation]);
    [Fact] void should_not_narrow_a_source_less_scan_to_a_synthetic_label() => _request.EventSourceId.ShouldBeNull();
    [Fact] void should_scan_after_the_requested_boundary() => _request.FromEventSequenceNumber.ShouldEqual(101UL);

    record Measured;
}
