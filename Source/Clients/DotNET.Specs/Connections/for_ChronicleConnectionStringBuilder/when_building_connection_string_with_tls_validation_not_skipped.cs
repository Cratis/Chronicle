// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_connection_string_with_tls_validation_not_skipped : Specification
{
    ChronicleConnectionStringBuilder _builder;
    string _url;

    void Establish()
    {
        _builder = new ChronicleConnectionStringBuilder
        {
            Host = "localhost",
            Port = 35000,
            SkipTlsValidation = false
        };
    }

    void Because() => _url = _builder.Build();

    [Fact] void should_include_skip_tls_validation_in_query_string() => _url.ShouldEqual("chronicle://localhost:35000?skipTlsValidation=false");
}
