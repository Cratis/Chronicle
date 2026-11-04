// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number_with_erased_properties;

public class with_a_failed_wire_response : given.an_event_sequence
{
    Exception _error;

    void Establish() => _sequences.FromSequenceNumberWithErasedProperties(Arg.Any<Contracts.Sequences.FromSequenceNumberWithErasedPropertiesRequest>(), Arg.Any<CallContext>()).Returns(new QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>> { ExceptionMessages = ["Synthetic query failure"], ExceptionStackTrace = "Synthetic stack" });

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetFromSequenceNumberWithErasedProperties(42));

    [Fact] void should_preserve_the_query_failure() => _error.ShouldBeOfExactType<QueryFailed>();
    [Fact] void should_preserve_the_failure_messages() => ((QueryFailed)_error).ExceptionMessages.ShouldEqual<IEnumerable<string>>(["Synthetic query failure"]);
}
