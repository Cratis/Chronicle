// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf;

namespace Cratis.Chronicle.Queries.for_QueryResult.given;

public class collection_query_results : Specification
{
    protected static QueryResult<IEnumerable<AppendedEventResponse>> RoundTrip(params AppendedEventResponse[] events)
    {
        var before = QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, events);
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, before);
        stream.Position = 0;
        return Serializer.Deserialize<QueryResult<IEnumerable<AppendedEventResponse>>>(stream);
    }
}
