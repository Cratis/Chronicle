// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_with_multiple_hosts : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://host1:35001,host2,host3:35003");

    [Fact] void should_have_first_host_as_host() => _builder.Host.ShouldEqual("host1");
    [Fact] void should_have_first_port_as_port() => _builder.Port.ShouldEqual(35001);
    [Fact] void should_have_three_server_addresses() => _builder.ServerAddresses.Count.ShouldEqual(3);
    [Fact] void should_have_first_server_address() => _builder.ServerAddresses[0].ShouldEqual(new ChronicleServerAddress("host1", 35001));
    [Fact] void should_have_second_server_address_with_default_port() => _builder.ServerAddresses[1].ShouldEqual(new ChronicleServerAddress("host2", 35000));
    [Fact] void should_have_third_server_address() => _builder.ServerAddresses[2].ShouldEqual(new ChronicleServerAddress("host3", 35003));
    [Fact] void should_not_be_srv() => _builder.IsSrv.ShouldBeFalse();
}
