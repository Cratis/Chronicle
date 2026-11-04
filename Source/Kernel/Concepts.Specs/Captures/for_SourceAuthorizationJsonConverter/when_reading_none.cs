// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Captures.for_SourceAuthorizationJsonConverter;

public class when_reading_none : Specification
{
    SourceAuthorization _result;

    void Because() => _result = JsonSerializer.Deserialize<SourceAuthorization>("""{"type":"none"}""");

    [Fact] void should_read_none() => _result.IsT3.ShouldBeTrue();
}
