// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_srv_url_with_name_server : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle+srv://cluster.example.com/?srvNameServer=127.0.0.1:5353");

    [Fact] void should_be_srv() => _builder.IsSrv.ShouldBeTrue();
    [Fact] void should_have_name_server() => _builder.SrvNameServer.ShouldEqual("127.0.0.1:5353");
    [Fact] void should_include_name_server_when_building() => _builder.Build().ShouldEqual("chronicle+srv://cluster.example.com:35000?srvNameServer=127.0.0.1%3A5353");
}
