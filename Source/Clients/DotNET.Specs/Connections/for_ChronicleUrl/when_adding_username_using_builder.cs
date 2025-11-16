// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_adding_username_using_builder : Specification
{
    ChronicleUrl _originalUrl;
    ChronicleUrl _newUrl;

    void Establish() => _originalUrl = new ChronicleUrl("chronicle://localhost:35000");

    void Because() => _newUrl = _originalUrl.WithUsername("admin");

    [Fact] void should_have_username() => _newUrl.Username.ShouldEqual("admin");
    [Fact] void should_preserve_host() => _newUrl.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_preserve_port() => _newUrl.ServerAddress.Port.ShouldEqual(35000);
    [Fact] void should_not_modify_original() => _originalUrl.Username.ShouldBeNull();
}
