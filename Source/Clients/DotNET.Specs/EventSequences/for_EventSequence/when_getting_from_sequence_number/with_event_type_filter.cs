// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class with_event_type_filter : given.an_event_sequence
{
    EventSequenceNumber _sequenceNumber;
    List<EventType> _filterEventTypes;
    Contracts.Sequences.FromSequenceNumberRequest _request;

    void Establish()
    {
        _sequenceNumber = 42UL;
        _filterEventTypes =
        [
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First),
            new(Guid.NewGuid().ToString(), EventTypeGeneration.First)
        ];

        _sequences
            .When(_ => _.FromSequenceNumber(Arg.Any<Contracts.Sequences.FromSequenceNumberRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<Contracts.Sequences.FromSequenceNumberRequest>());

        _sequences
            .FromSequenceNumber(Arg.Any<Contracts.Sequences.FromSequenceNumberRequest>(), CallContext.Default)
            .Returns(QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.NewGuid(), []));
    }

    async Task Because() => await _eventSequence.GetFromSequenceNumber(_sequenceNumber, filterEventTypes: _filterEventTypes);

    [Fact] void should_pass_event_types() => _request.EventTypeIds.ShouldEqual(string.Join(',', _filterEventTypes.Select(_ => _.Id.Value)));
    [Fact] void should_pass_sequence_number() => _request.FromEventSequenceNumber.ShouldEqual((ulong)_sequenceNumber);
}
