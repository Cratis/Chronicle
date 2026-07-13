// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_with_multiple_hosts_and_authentication : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://admin:secret@host1,host2:35002/?disableTls=true");

    [Fact] void should_have_username() => _builder.Username.ShouldEqual("admin");
    [Fact] void should_have_password() => _builder.Password.ShouldEqual("secret");
    [Fact] void should_have_two_server_addresses() => _builder.ServerAddresses.Count.ShouldEqual(2);
    [Fact] void should_have_first_server_address_with_default_port() => _builder.ServerAddresses[0].ShouldEqual(new ChronicleServerAddress("host1", 35000));
    [Fact] void should_have_second_server_address() => _builder.ServerAddresses[1].ShouldEqual(new ChronicleServerAddress("host2", 35002));
    [Fact] void should_have_tls_disabled() => _builder.DisableTls.ShouldBeTrue();
}
