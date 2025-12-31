// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_adding_api_key_using_builder : Specification
{
    ChronicleConnectionString _originalUrl;
    ChronicleConnectionString _newUrl;

    void Establish() => _originalUrl = new ChronicleConnectionString("chronicle://localhost:35000");

    void Because() => _newUrl = _originalUrl.WithApiKey("mykey123");

    [Fact] void should_have_api_key() => _newUrl.ApiKey.ShouldEqual("mykey123");
    [Fact] void should_have_api_key_auth_mode() => _newUrl.AuthenticationMode.ShouldEqual(AuthenticationMode.ApiKey);
    [Fact] void should_preserve_host() => _newUrl.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_preserve_port() => _newUrl.ServerAddress.Port.ShouldEqual(35000);
}
