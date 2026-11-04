// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Captures.for_SourceAuthorizationJsonConverter;

public class when_reading_a_non_string_type : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => JsonSerializer.Deserialize<SourceAuthorization>("""{"type":1}"""));

    [Fact] void should_reject_it() => _error.ShouldBeOfExactType<JsonException>();
}
