// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Captures.for_SourceAuthorizationJsonConverter;

public class when_reading_an_unknown_type : Specification
{
    const string MisplacedSecret = "synthetic-secret-value";

    Exception _error;

    void Because() => _error = Catch.Exception(() => JsonSerializer.Deserialize<SourceAuthorization>($$"""{"type":"{{MisplacedSecret}}","token":"value"}"""));

    [Fact] void should_reject_it() => _error.ShouldBeOfExactType<JsonException>();
    [Fact] void should_not_echo_the_type_value() => _error.Message.Contains(MisplacedSecret, StringComparison.Ordinal).ShouldBeFalse();
}
