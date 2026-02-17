// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_parsing_url_without_authentication : Specification
{
    ChronicleConnectionString _url;

    void Establish() => _url = new ChronicleConnectionString("chronicle://localhost:35000");

    [Fact] void should_have_correct_host() => _url.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_have_correct_port() => _url.ServerAddress.Port.ShouldEqual(35000);
    [Fact] void should_not_have_username() => _url.Username.ShouldBeNull();
    [Fact] void should_not_have_password() => _url.Password.ShouldBeNull();
}
