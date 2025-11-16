// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_using_custom_port : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 8080
        };
    }

    void Because() => _url = _builder.BuildChronicleUrl();

    [Fact] void should_include_port_in_url() => _url.ShouldEqual("chronicle://localhost:8080");
}
