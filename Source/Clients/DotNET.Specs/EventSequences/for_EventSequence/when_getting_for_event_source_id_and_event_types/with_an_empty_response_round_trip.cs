// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_for_event_source_id_and_event_types;

public class with_an_empty_response_round_trip : Specification
{
    QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>> _result;

    void Because()
    {
        var before = QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.Empty, []);
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, before);
        stream.Position = 0;
        _result = Serializer.Deserialize<QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>>(stream);
    }

    [Fact] void should_stay_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_deserialize_an_empty_collection() => _result.Data.ShouldBeEmpty();
}
