// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_redacted_connection_string_without_credentials : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "host1",
            Port = 35001,
            LoadBalancer = "RoundRobin"
        };
    }

    void Because() => _url = _builder.BuildRedacted();

    [Fact] void should_be_identical_to_the_unredacted_connection_string() => _url.ShouldEqual(_builder.Build());
    [Fact] void should_keep_scheme_host_and_port() => _url.ShouldEqual("chronicle://host1:35001?loadBalancer=RoundRobin");
}
