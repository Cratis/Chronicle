// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Captures.for_SourceAuthorizationJsonConverter;

public class when_round_tripping_oauth : Specification
{
    SourceOAuthAuthorization _authorization;
    SourceAuthorization _result;

    void Establish() => _authorization = new("https://authority.example", "client", "secret");

    void Because() => _result = JsonSerializer.Deserialize<SourceAuthorization>(JsonSerializer.Serialize<SourceAuthorization>(_authorization));

    [Fact] void should_keep_the_authorization() => _result.AsT2.ShouldEqual(_authorization);
}
