// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_url_with_tls_disabled : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000,
            DisableTls = true
        };
    }

    void Because() => _url = _builder.BuildChronicleUrl();

    [Fact] void should_include_disable_tls_in_query_string() => _url.ShouldEqual("chronicle://localhost?disableTls=true");
}
