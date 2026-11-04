// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Captures.for_SourceAuthorizationJsonConverter;

public class when_reading_basic : Specification
{
    SourceAuthorization _result;

    void Because() => _result = JsonSerializer.Deserialize<SourceAuthorization>("""{"type":"basic","username":"user","password":"password"}""");

    [Fact] void should_read_the_username() => _result.AsT0.Username.ShouldEqual("user");
    [Fact] void should_read_the_password() => _result.AsT0.Password.ShouldEqual("password");
}
