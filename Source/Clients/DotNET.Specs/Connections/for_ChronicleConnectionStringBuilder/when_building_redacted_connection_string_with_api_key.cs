// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_redacted_connection_string_with_api_key : Specification
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

    void Because() => _url = _builder.BuildRedacted();

    [Fact] void should_mask_the_api_key() => _url.ShouldEqual("chronicle://localhost:35000?apiKey=***");
    [Fact] void should_not_expose_the_api_key() => _url.Contains("my-api-key", StringComparison.Ordinal).ShouldBeFalse();
}
