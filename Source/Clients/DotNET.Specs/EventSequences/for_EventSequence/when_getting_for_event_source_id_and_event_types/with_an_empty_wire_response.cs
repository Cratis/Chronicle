// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_an_empty_wire_response : given.an_event_sequence_with_a_wire_response
{
    IImmutableList<AppendedEvent> _result;

    void Establish() => RespondWith(QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.Empty, []));

    async Task Because() => _result = await _eventSequence.GetForEventSourceIdAndEventTypes("synthetic-source", []);

    [Fact] void should_receive_a_successful_query() => _wireResponse.IsSuccess.ShouldBeTrue();
    [Fact] void should_deserialize_an_empty_collection() => _wireResponse.Data.ShouldBeEmpty();
    [Fact] void should_return_an_empty_history() => _result.ShouldBeEmpty();
}
