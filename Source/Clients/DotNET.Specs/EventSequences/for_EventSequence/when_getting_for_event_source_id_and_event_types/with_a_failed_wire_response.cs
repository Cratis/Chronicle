// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_a_failed_wire_response : given.an_event_sequence_with_a_wire_response
{
    Exception _error;

    void Establish() => RespondWith(new() { ExceptionMessages = ["Synthetic query failure"] });

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetForEventSourceIdAndEventTypes("synthetic-source", []));

    [Fact] void should_preserve_the_query_failure() => _error.ShouldBeOfExactType<QueryFailed>();
    [Fact] void should_receive_no_data() => _wireResponse.Data.ShouldBeNull();
}
