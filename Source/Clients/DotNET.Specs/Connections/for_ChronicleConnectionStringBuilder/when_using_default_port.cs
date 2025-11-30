// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_using_default_port : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost"
        };
    }

    void Because() => _url = _builder.BuildChronicleUrl();

    [Fact] void should_get_default_port() => _builder.Port.ShouldEqual(35000);
    [Fact] void should_not_include_port_in_url() => _url.ShouldEqual("chronicle://localhost");
}
