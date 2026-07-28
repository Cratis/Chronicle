// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_connection_string_with_multiple_hosts : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish() => _builder = new ChronicleConnectionStringBuilder()
        .WithServerAddresses(new ChronicleServerAddress("host1", 35001), new ChronicleServerAddress("host2"));

    void Because() => _url = _builder.Build();

    [Fact] void should_include_all_hosts_in_url() => _url.ShouldEqual("chronicle://host1:35001,host2:35000");
    [Fact] void should_round_trip_through_parsing() => new ChronicleConnectionStringBuilder(_url).ServerAddresses.ShouldEqual(_builder.ServerAddresses);
}
