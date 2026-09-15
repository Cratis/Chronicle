// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number;

public class with_an_invalid_wire_response : given.an_event_sequence_with_a_wire_response
{
    Exception _error;

    void Establish() => RespondWith(new() { ValidationResults = [new() { Message = "Synthetic validation failure" }] });

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First));

    [Fact] void should_preserve_the_query_failure() => _error.ShouldBeOfExactType<QueryFailed>();
    [Fact] void should_preserve_the_validation_results() => ((QueryFailed)_error).ValidationResults.ShouldEqual(_wireResponse.ValidationResults);
}
