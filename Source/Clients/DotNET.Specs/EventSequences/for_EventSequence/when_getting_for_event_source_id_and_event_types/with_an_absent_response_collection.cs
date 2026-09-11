// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_an_absent_response_collection : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    IImmutableList<AppendedEvent> _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();

        _sequences
            .ForEventSourceIdAndEventTypes(Arg.Any<Contracts.Sequences.ForEventSourceIdAndEventTypesRequest>(), CallContext.Default)
            .Returns(QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.NewGuid(), null!));
    }

    async Task Because() => _result = await _eventSequence.GetForEventSourceIdAndEventTypes(_eventSourceId, []);

    [Fact] void should_return_an_empty_collection() => _result.ShouldBeEmpty();
}
