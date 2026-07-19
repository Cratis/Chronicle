// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_srv_url : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle+srv://cluster.example.com");

    [Fact] void should_have_srv_scheme() => _builder.Scheme.ShouldEqual("chronicle+srv");
    [Fact] void should_be_srv() => _builder.IsSrv.ShouldBeTrue();
    [Fact] void should_have_correct_host() => _builder.Host.ShouldEqual("cluster.example.com");
    [Fact] void should_have_default_port() => _builder.Port.ShouldEqual(35000);
}
