// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_parsing_url_with_default_port : Specification
{
    ChronicleUrl _url;

    void Establish() => _url = new ChronicleUrl("chronicle://localhost");

    [Fact] void should_have_correct_host() => _url.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_use_default_port() => _url.ServerAddress.Port.ShouldEqual(35000);
}
