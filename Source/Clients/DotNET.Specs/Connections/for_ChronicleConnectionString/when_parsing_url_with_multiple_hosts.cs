// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_multiple_hosts : Specification
{
    ChronicleConnectionString _connectionString;

    void Establish() => _connectionString = new ChronicleConnectionString("chronicle://host1:35001,host2:35002");

    [Fact] void should_have_first_address_as_server_address() => _connectionString.ServerAddress.ShouldEqual(new ChronicleServerAddress("host1", 35001));
    [Fact] void should_have_two_server_addresses() => _connectionString.ServerAddresses.Count.ShouldEqual(2);
    [Fact] void should_have_second_server_address() => _connectionString.ServerAddresses[1].ShouldEqual(new ChronicleServerAddress("host2", 35002));
    [Fact] void should_not_be_srv() => _connectionString.IsSrv.ShouldBeFalse();
}
