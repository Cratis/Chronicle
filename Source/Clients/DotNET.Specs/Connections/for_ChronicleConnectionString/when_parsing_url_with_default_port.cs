// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_default_port : Specification
{
    ChronicleConnectionString _url;

    void Establish() => _url = new ChronicleConnectionString("chronicle://localhost");

    [Fact] void should_have_correct_host() => _url.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_use_default_port() => _url.ServerAddress.Port.ShouldEqual(35000);
}
