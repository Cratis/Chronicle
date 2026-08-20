// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;
using ProtoBuf;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_generated_contracts;

public class when_serializing_date_time_offset_response : Specification
{
    DateTimeOffset _createdAt;
    DateTimeOffset _lastModifiedAt;
    ApplicationResponse _result = null!;

    void Establish()
    {
        _createdAt = DateTimeOffset.UtcNow;
        _lastModifiedAt = _createdAt.AddMinutes(1);
    }

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ApplicationResponse
        {
            CreatedAt = _createdAt,
            LastModifiedAt = _lastModifiedAt
        });
        stream.Position = 0;
        _result = Serializer.Deserialize<ApplicationResponse>(stream);
    }

    [Fact] void should_round_trip_created_at() => ((DateTimeOffset)_result.CreatedAt).ShouldEqual(_createdAt);
    [Fact] void should_round_trip_last_modified_at() => ((DateTimeOffset?)_result.LastModifiedAt).ShouldEqual(_lastModifiedAt);
}
