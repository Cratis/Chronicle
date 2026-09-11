// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf;

namespace Cratis.Chronicle.Queries.for_QueryResult.when_round_tripping;

public class with_a_missing_single_entity : Specification
{
    QueryResult<AppendedEventResponse?> _result;

    void Because()
    {
        var before = QueryResult<AppendedEventResponse?>.Success(Guid.Empty, null);
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, before);
        stream.Position = 0;
        _result = Serializer.Deserialize<QueryResult<AppendedEventResponse?>>(stream);
    }

    [Fact] void should_stay_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_missing_entity() => _result.EnsureSuccess().ShouldBeNull();
}
