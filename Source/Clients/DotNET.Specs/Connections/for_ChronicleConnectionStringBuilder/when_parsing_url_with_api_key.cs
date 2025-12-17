// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_with_api_key : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost:35000/?apiKey=my-api-key");

    [Fact] void should_have_api_key() => _builder.ApiKey.ShouldEqual("my-api-key");
    [Fact] void should_have_api_key_authentication_mode() => _builder.AuthenticationMode.ShouldEqual(AuthenticationMode.ApiKey);
}
