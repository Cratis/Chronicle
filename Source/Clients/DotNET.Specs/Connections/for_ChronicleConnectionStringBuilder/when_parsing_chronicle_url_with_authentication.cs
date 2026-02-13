// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_chronicle_url_with_authentication : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://admin:secret@localhost:35000");

    [Fact] void should_have_correct_host() => _builder.Host.ShouldEqual("localhost");
    [Fact] void should_have_correct_port() => _builder.Port.ShouldEqual(35000);
    [Fact] void should_have_username() => _builder.Username.ShouldEqual("admin");
    [Fact] void should_have_password() => _builder.Password.ShouldEqual("secret");
    [Fact] void should_have_correct_scheme() => _builder.Scheme.ShouldEqual("chronicle");
}
