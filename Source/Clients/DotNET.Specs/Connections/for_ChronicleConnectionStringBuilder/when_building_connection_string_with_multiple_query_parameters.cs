// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_connection_string_with_multiple_query_parameters : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000,
            ApiKey = "my-api-key",
            DisableTls = true
        };
    }

    void Because() => _url = _builder.Build();

    [Fact] void should_include_all_query_parameters() => _url.ShouldEqual("chronicle://localhost:35000?apiKey=my-api-key&disableTls=true");
}
