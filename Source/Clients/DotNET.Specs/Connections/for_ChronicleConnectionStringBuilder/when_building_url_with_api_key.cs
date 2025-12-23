// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_url_with_api_key : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000,
            ApiKey = "my-api-key"
        };
    }

    void Because() => _url = _builder.BuildChronicleUrl();

    [Fact] void should_include_api_key_in_query_string() => _url.ShouldEqual("chronicle://localhost?apiKey=my-api-key");
}
