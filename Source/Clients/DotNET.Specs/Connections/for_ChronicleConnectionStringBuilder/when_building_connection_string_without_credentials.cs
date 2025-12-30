// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_connection_string_without_credentials : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000
        };
    }

    void Because() => _url = _builder.Build();

    [Fact] void should_not_include_credentials_in_url() => _url.ShouldEqual("chronicle://localhost:35000");
}
