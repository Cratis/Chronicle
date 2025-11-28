// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_parsing_url_without_authentication : Specification
{
    ChronicleUrl _url;

    void Establish() => _url = new ChronicleUrl("chronicle://localhost:35000");

    [Fact] void should_have_correct_host() => _url.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_have_correct_port() => _url.ServerAddress.Port.ShouldEqual(35000);
    [Fact] void should_not_have_username() => _url.Username.ShouldBeNull();
    [Fact] void should_not_have_password() => _url.Password.ShouldBeNull();
}
